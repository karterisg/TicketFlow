using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.API.Repositories;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets/{ticketId}/activities")]
public class TicketActivityController(ITicketActivityRepository activityRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int ticketId)
    {
        var ticketActivities = await activityRepository.GetByTicketIdAsync(ticketId);
        
        var dtos = ticketActivities.Select(a=> new TicketActivityDto
        {
            Id = a.Id,
            ActionType = a.ActionType.ToString(),
            Description = a.Description,
            ActorName = a.ActorName,
            CreatedAt = a.CreatedAt
        });
        return Ok(dtos);
    }
}