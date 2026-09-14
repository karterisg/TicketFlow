using TicketFlow.API.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketFlow.API.Jobs;

public class NotificationJobs(AppDbContext context, ILogger<NotificationJobs> logger)
{
    public async Task CleanupReadNotificationsAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-3);
        var deleted = await context.Notifications
            .Where(n => n.ReadAt != null && n.ReadAt < cutoff)
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {Count} read notifications older than 3 days", deleted);
    }
}
