using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;
public interface IProjectAccessService
{
    Task<ProjectRole?> GetMemberRoleAsync(int projectId, int? userId, int? agentId);
    Task EnsureRoleAsync(int projectId, int? userId, int? agentId, ProjectRole minimumRole);
}
