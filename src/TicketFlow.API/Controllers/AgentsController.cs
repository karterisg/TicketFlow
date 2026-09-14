using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Extensions;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AgentsController(AppDbContext context, ILogger<AgentsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Manager,Agent")]
    public async Task<IActionResult> GetAll()
    {
        var agents = await context.Agents
            .Include(a => a.AssignedTickets)
            .ToListAsync();
        return Ok(agents.ToDtoList());
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Manager,Agent")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string? search = null)
    {
        logger.LogInformation("Fetching agents page {Page} (size {PageSize}, search='{Search}')", page, pageSize, search);

        var query = context.Agents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.FullName.Contains(search) || a.Email.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AgentDto
            {
                Id = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                IsAvailable = a.IsAvailable,
                AssignedTicketCount = a.AssignedTickets.Count
            })
            .ToListAsync();

        logger.LogInformation("Returned {Count}/{Total} agents for page {Page}", items.Count, totalCount, page);

        return Ok(new PagedResultDto<AgentDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Agent")]
    public async Task<IActionResult> GetById(int id)
    {
        var agent = await context.Agents
            .Include(a => a.AssignedTickets)
            .FirstOrDefaultAsync(a => a.Id == id);
        return agent is null ? NotFound() : Ok(agent.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = "Perm:Agents.Manage")]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto)
    {
        var agent = new Agent { FullName = dto.FullName, Email = dto.Email };
        context.Agents.Add(agent);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = agent.Id }, agent.ToDto());
    }

    [HttpPatch("{id}/Availability")]
    public async Task<IActionResult> SetAvailability(int id, [FromBody] SetAvailabilityRequest request)
    {
        if (!User.IsInRole("Manager") && id != User.GetAgentId())
            return Forbid();

        var agent = await context.Agents.FindAsync(id);
        if (agent is null) return NotFound();
        agent.IsAvailable = request.IsAvailable;
        await context.SaveChangesAsync();
        return NoContent();
    }
}

public record SetAvailabilityRequest(bool IsAvailable);
