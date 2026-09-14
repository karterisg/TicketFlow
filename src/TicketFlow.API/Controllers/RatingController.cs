using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets/{ticketId}/rating")]
public class RatingController(IRatingService ratingService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(int ticketId, [FromBody] SubmitRatingDto dto)
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var rating = await ratingService.SubmitRatingAsync(ticketId, userId.Value, dto.Score, dto.Comment);
        return Ok(rating.ToDto());
    }

    [HttpGet]
    public async Task<IActionResult> Get(int ticketId)
    {
        var rating = await ratingService.GetRatingAsync(ticketId);
        return rating is null ? NotFound() : Ok(rating.ToDto());
    }
}
