using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MeetingsController(IMeetingService meetingService) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? projectId, [FromQuery] int? teamId)
    {
        var meetings = await meetingService.GetAllMeetingsAsync(projectId, teamId);
        return Ok(meetings);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null,
        [FromQuery] int? forUserId = null, [FromQuery] int? forAgentId = null)
    {
        var result = await meetingService.GetPagedMeetingsAsync(page, pageSize, search, forUserId, forAgentId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var meeting = await meetingService.GetMeetingByIdAsync(id);
        return meeting is null ? NotFound() : Ok(meeting);
    }

    [HttpPost]
    [Authorize(Policy = "Perm:Meetings.Manage")]
    public async Task<IActionResult> Create([FromBody] CreateMeetingDto dto)
    {
        var meeting = await meetingService.CreateMeetingAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = meeting.Id }, meeting.ToDto());
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "Perm:Meetings.Manage")]
    public async Task<IActionResult> Cancel(int id)
    {
        await meetingService.CancelMeetingAsync(id, User.GetUserId(), User.IsInRole("Manager"));
        return NoContent();
    }

    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondToMeetingDto dto)
    {
        await meetingService.RespondAsync(id, dto);
        return NoContent();
    }
}
