using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Ticket> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, List<string>? statuses,
        int? forUserId, int? forAgentId, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
