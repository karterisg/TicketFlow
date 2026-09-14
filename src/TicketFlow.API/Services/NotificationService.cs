using Microsoft.AspNetCore.SignalR;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task NotifyAsync(int userId, string message, string type)
    {
        var notification = new Notification(userId, message, type);
        await notificationRepository.AddAsync(notification);

        await hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", new { message, type });
    }

    public async Task<List<NotificationDto>> GetForUserAsync(int userId)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(userId);
        return notifications.ToDtoList();
    }

    public Task<int> GetUnreadCountAsync(int userId) => notificationRepository.GetUnreadCountAsync(userId);

    public Task MarkAllReadAsync(int userId) => notificationRepository.MarkAllAsReadAsync(userId);
}
