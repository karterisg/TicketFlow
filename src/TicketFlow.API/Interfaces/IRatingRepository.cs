using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface IRatingRepository
{
    Task AddAsync(Rating rating, CancellationToken cancellationToken = default);
    Task<Rating?> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);
}
