using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Interfaces;

public interface ITeamService
{
    Task<IEnumerable<TeamDto>> GetAllTeamsAsync(int? forUserId = null, int? forAgentId = null);
    Task<PagedResultDto<TeamDto>> GetPagedTeamsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null);
    Task<TeamDto?> GetTeamByIdAsync(int id);
    Task<Team> CreateTeamAsync(CreateTeamDto dto);
    Task DeleteTeamAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager);
    Task AssignTeamToProjectAsync(int projectId, int teamId, int? actingUserId, int? actingAgentId, bool isGlobalManager);
    Task UnassignTeamFromProjectAsync(int projectId, int teamId, int? actingUserId, int? actingAgentId, bool isGlobalManager);
}
