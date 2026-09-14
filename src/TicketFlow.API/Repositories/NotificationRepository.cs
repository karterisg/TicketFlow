using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories
{
    public class NotificationRepository(AppDbContext context) : INotificationRepository
    {
        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        {
            return context.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .CountAsync(cancellationToken);
        }

        public Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return context.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadAt, now), cancellationToken);
        }

        public Task<int> DeleteReadOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            return context.Notifications
                .Where(n => n.ReadAt != null && n.ReadAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
