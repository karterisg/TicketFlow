using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface IMeetingRepository
{
    Task<IEnumerable<Meeting>> GetAllAsync(int? projectId, int? teamId, CancellationToken cancellationToken = default);
    Task<(List<Meeting> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default);
    Task<Meeting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Meeting> CreateAsync(Meeting meeting, CancellationToken cancellationToken = default);
    Task UpdateAsync(Meeting meeting, CancellationToken cancellationToken = default);
}
