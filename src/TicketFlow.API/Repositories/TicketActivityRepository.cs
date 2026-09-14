using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories
{
    public class TicketActivityRepository(AppDbContext context) : ITicketActivityRepository
    {
        public async Task AddAsync(TicketActivity ticketActivity, CancellationToken cancellationToken = default)
        {
            context.TicketActivities.Add(ticketActivity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<TicketActivity>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default)
        {
            return await context.TicketActivities
                .AsNoTracking()
                .Where(a => a.TicketId == ticketId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
