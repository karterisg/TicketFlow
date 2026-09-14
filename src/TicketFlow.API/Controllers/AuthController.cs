using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Services;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    AppDbContext context) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var existingUser = await userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return Conflict(new { message = "Email is already in use." });

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = dto.Role,
            UserName = dto.Email
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, dto.Role);

        int? userId = null;
        int? agentId = null;
        if (dto.Role == "Customer" || dto.Role == "Manager")
        {
            var customUser = new User { FullName = dto.FullName, Email = dto.Email };
            context.Users.Add(customUser);
            await context.SaveChangesAsync();
            userId = customUser.Id;
        }
        else if (dto.Role == "Agent") //now going on db
        {
            var agent = new Agent { FullName = dto.FullName, Email = dto.Email };
            context.Agents.Add(agent);
            await context.SaveChangesAsync();
            agentId = agent.Id;
        }

        var token = tokenService.GenerateToken(user, userId, agentId);
        return Ok(new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email!,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = userId
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var customUser = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Self-heal: older Manager accounts were created before Managers got a linked domain User row.
        if (customUser is null && user.Role == "Manager")
        {
            customUser = new User { FullName = user.FullName, Email = user.Email! };
            context.Users.Add(customUser);
            await context.SaveChangesAsync();
        }

        int? agentId = null;
        string? appUserId = null;

        if (user.Role == "Agent")
        {
            var agent = await context.Agents.FirstOrDefaultAsync(a => a.Email == dto.Email);
            agentId = agent?.Id;
            appUserId = user.Id;
        }

        return Ok(new AuthResponseDto
        {
            Token = tokenService.GenerateToken(user, customUser?.Id, agentId),
            FullName = user.FullName,
            Email = user.Email!,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = customUser?.Id,
            AgentId = agentId,
            AppUserId = appUserId
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (applicationUserId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(applicationUserId);
        if (user is null) return Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }
}
