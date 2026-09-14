using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface ITeamAccessService
{
    Task<TeamRole?> GetMemberRoleAsync(int teamId, int? userId, int? agentId);
    Task EnsureRoleAsync(int teamId, int? userId, int? agentId, TeamRole minimumRole);
}
