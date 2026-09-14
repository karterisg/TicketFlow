using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class NotificationMapper
{
    public static NotificationDto ToDto(this Notification notification) => new()
    {
        Id = notification.Id,
        Message = notification.Message,
        Type = notification.Type,
        CreatedAt = notification.CreatedAt,
        ReadAt = notification.ReadAt
    };

    public static List<NotificationDto> ToDtoList(this IEnumerable<Notification> notifications)
        => notifications.Select(n => n.ToDto()).ToList();
}
