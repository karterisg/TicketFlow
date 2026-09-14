using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories
{
    public class RatingRepository(AppDbContext context) : IRatingRepository
    {
        public async Task AddAsync(Rating rating, CancellationToken cancellationToken = default)
        {
            context.Ratings.Add(rating);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Rating?> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default)
        {
            return await context.Ratings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.TicketId == ticketId, cancellationToken);
        }

        public async Task<Rating?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await context.Ratings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<List<Rating>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await context.Ratings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(Rating rating, CancellationToken cancellationToken = default)
        {
            context.Ratings.Update(rating);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
