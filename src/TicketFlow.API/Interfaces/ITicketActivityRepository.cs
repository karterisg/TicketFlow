using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface ITicketActivityRepository
{
    Task AddAsync(TicketActivity ticketActivity, CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketActivity>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);
}