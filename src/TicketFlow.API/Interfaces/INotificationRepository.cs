using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> DeleteReadOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}
