using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Extensions;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Route("api/tickets/{ticketId}/time-entries")]
[Authorize]
public class TimeEntriesController(AppDbContext context) : ControllerBase
{
    // GET — όλα τα time entries ενός ticket
    [HttpGet]
    public async Task<IActionResult> GetAll(int ticketId)
    {
        var entries = await context.TimeEntries
            .Include(e => e.Agent)
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();

        // TotalMinutes is computed in C# (not translatable to SQL), so map after materialization
        var dtos = entries.Select(e => new TimeEntryDto
        {
            Id = e.Id,
            TicketId = e.TicketId,
            AgentId = e.AgentId,
            AgentName = e.Agent.FullName,
            StartedAt = e.StartedAt,
            EndedAt = e.EndedAt,
            TotalMinutes = e.TotalMinutes,
            Note = e.Note
        });

        return Ok(dtos);
    }

    // POST /start — ξεκινάει timer
    [HttpPost("start")]
    [Authorize(Roles = "Agent,Manager")]
    public async Task<IActionResult> Start(int ticketId, [FromBody] StartTimerDto dto)
    {
        if (!User.IsInRole("Manager") && dto.AgentId != User.GetAgentId())
            return Forbid();

        var agentExists = await context.Agents.AnyAsync(a => a.Id == dto.AgentId);
        if (!agentExists)
            return NotFound($"Agent {dto.AgentId} not found.");

        // Έλεγχος αν υπάρχει ήδη running timer για αυτόν τον agent
        var running = await context.TimeEntries
            .AnyAsync(e => e.TicketId == ticketId
                        && e.AgentId == dto.AgentId
                        && e.EndedAt == null
                        && e.ManualMinutes == null);

        if (running)
            return BadRequest("Timer is already running for this ticket.");

        var entry = new TimeEntry
        {
            TicketId = ticketId,
            AgentId = dto.AgentId,
            StartedAt = DateTime.UtcNow
        };

        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        return Ok(new { entry.Id, entry.StartedAt });
    }

    // POST /stop 
    [HttpPost("stop")]
    [Authorize(Roles = "Agent,Manager")]
    public async Task<IActionResult> Stop(int ticketId, [FromBody] StopTimerDto dto)
    {
        if (!User.IsInRole("Manager") && dto.AgentId != User.GetAgentId())
            return Forbid();

        var entry = await context.TimeEntries
            .FirstOrDefaultAsync(e => e.TicketId == ticketId
                                   && e.AgentId == dto.AgentId
                                   && e.EndedAt == null
                                   && e.ManualMinutes == null);

        if (entry is null)
            return NotFound("No running timer found.");

        entry.EndedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new { entry.Id, entry.EndedAt, entry.TotalMinutes });
    }

    // POST /log 
    [HttpPost("log")]
    [Authorize(Roles = "Agent,Manager")]
    public async Task<IActionResult> Log(int ticketId, [FromBody] LogTimeDto dto)
    {
        if (!User.IsInRole("Manager") && dto.AgentId != User.GetAgentId())
            return Forbid();

        if (dto.Minutes <= 0)
            return BadRequest("Minutes must be greater than 0.");

        var agentExists = await context.Agents.AnyAsync(a => a.Id == dto.AgentId);
        if (!agentExists)
            return NotFound($"Agent {dto.AgentId} not found.");

        var entry = new TimeEntry
        {
            TicketId = ticketId,
            AgentId = dto.AgentId,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow,
            ManualMinutes = dto.Minutes,
            Note = dto.Note
        };

        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        return Ok(new TimeEntryDto
        {
            Id = entry.Id,
            TicketId = entry.TicketId,
            AgentId = entry.AgentId,
            TotalMinutes = entry.TotalMinutes,
            Note = entry.Note
        });
    }
}