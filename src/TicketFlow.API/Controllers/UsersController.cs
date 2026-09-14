using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TicketFlow.API.Data;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize(Policy = "Perm:Users.Manage")]
[Route("api/[controller]")]
public class UsersController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null)
    {
        logger.LogInformation("Fetching users page {Page} (size {PageSize}, search='{Search}')", page, pageSize, search);

        var query = context.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto { Id = u.Id, FullName = u.FullName, Email = u.Email, TicketCount = u.Tickets.Count })
            .ToListAsync();

        var emails = items.Select(u => u.Email).ToList();
        var identities = await context.Set<ApplicationUser>()
            .Where(au => au.Email != null && emails.Contains(au.Email))
            .ToDictionaryAsync(au => au.Email!, au => new { au.Role, au.Id });

        foreach (var item in items)
            if (identities.TryGetValue(item.Email, out var identity))
            {
                item.Role = identity.Role;
                item.AppUserId = identity.Id;
            }

        logger.LogInformation("Returned {Count}/{Total} users for page {Page}", items.Count, totalCount, page);

        return Ok(new PagedResultDto<UserDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await context.Users
            .Include(u => u.Tickets)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{id}/promote-to-agent")]
    public async Task<IActionResult> PromoteToAgent(int id)
    {
        var user = await context.Users.Include(u => u.Tickets).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        if (user.Tickets.Count > 0)
            return Conflict("Cannot promote a user with existing tickets.");

        var agentExists = await context.Agents.AnyAsync(a => a.Email == user.Email);
        if (agentExists)
            return Conflict("An agent with this email already exists.");

        var identityUser = await userManager.FindByEmailAsync(user.Email);
        if (identityUser is not null && identityUser.Role != "Customer")
            return Conflict($"Cannot promote a user with role '{identityUser.Role}' to Agent.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (identityUser is not null)
            {
                await userManager.RemoveFromRoleAsync(identityUser, "Customer");
                await userManager.AddToRoleAsync(identityUser, "Agent");
                identityUser.Role = "Agent";
                await userManager.UpdateAsync(identityUser);
            }

            var agent = new Agent { FullName = user.FullName, Email = user.Email };
            context.Agents.Add(agent);

            var memberships = await context.ProjectMembers.Where(m => m.UserId == user.Id).ToListAsync();
            foreach (var membership in memberships)
            {
                membership.UserId = null;
                membership.Agent = agent;
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new AgentDto { Id = agent.Id, FullName = agent.FullName, Email = agent.Email, IsAvailable = agent.IsAvailable });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var exists = await context.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
            return Conflict("A user with this email already exists.");

        var identityExists = await userManager.FindByEmailAsync(dto.Email);
        if (identityExists is not null)
            return Conflict("A user with this email already exists.");

        // Create Identity account so the user can login
        var appUser = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            Role = "Customer"
        };
        var result = await userManager.CreateAsync(appUser, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(appUser, "Customer");

        var user = new User { FullName = dto.FullName, Email = dto.Email };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new UserDto { Id = user.Id, FullName = user.FullName, Email = user.Email });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();

        var isTeamMember = await context.Set<TeamMember>().AnyAsync(m => m.UserId == id);
        var isProjectMember = await context.Set<ProjectMember>().AnyAsync(m => m.UserId == id);
        var hasTickets = await context.Tickets.AnyAsync(t => t.UserId == id);
        if (isTeamMember || isProjectMember || hasTickets)
            return Conflict("Cannot delete a user who is still a team/project member or has tickets.");

        var identityUser = await userManager.FindByEmailAsync(user.Email);
        if (identityUser is not null)
            await userManager.DeleteAsync(identityUser);

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return NoContent();
    }
}