using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface ITeamRepository
{
    Task<IEnumerable<Team>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Team> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default);
    Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
