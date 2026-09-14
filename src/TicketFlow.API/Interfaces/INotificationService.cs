using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(int userId, string message, string type);
    Task<List<NotificationDto>> GetForUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAllReadAsync(int userId);
}
