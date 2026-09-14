using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(int? forUserId = null, int? forAgentId = null);
    Task<PagedResultDto<ProjectDto>> GetPagedProjectsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null);
    Task<ProjectDto?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(CreateProjectDto dto);
    Task<Project> CreateProjectFromTemplateAsync(CreateProjectFromTemplateDto dto);
    Task ArchiveProjectAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager);
    Task DeleteProjectAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager);
    Task UpdateSettingsAsync(int id, UpdateProjectSettingsDto dto, int? actingUserId, int? actingAgentId, bool isGlobalManager);
}
