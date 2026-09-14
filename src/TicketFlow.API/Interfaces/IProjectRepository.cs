using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Project> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
