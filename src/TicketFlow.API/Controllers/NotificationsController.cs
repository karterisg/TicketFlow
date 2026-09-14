using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var notifications = await notificationService.GetForUserAsync(userId.Value);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var count = await notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        await notificationService.MarkAllReadAsync(userId.Value);
        return Ok();
    }
}
