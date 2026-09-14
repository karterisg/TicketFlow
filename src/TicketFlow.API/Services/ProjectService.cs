using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    AppDbContext context,
    ICacheService cache,
    ILogger<ProjectService> logger,
    IHubContext<NotificationHub> hubContext,
    IProjectAccessService accessService) : IProjectService
{
    private const string AllProjectsCacheKey = "projects:all";

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(int? forUserId = null, int? forAgentId = null)
    {
        logger.LogInformation("Fetching all projects");

        if (forUserId.HasValue)
        {
            var projects = await projectRepository.GetAllAsync();
            var mine = projects.Where(p => p.OwnerUserId == forUserId.Value
                || p.Members.Any(m => m.UserId == forUserId.Value));
            return mine.ToDtoList();
        }

        if (forAgentId.HasValue)
        {
            var projects = await projectRepository.GetAllAsync();
            var mine = projects.Where(p => p.Members.Any(m => m.AgentId == forAgentId.Value));
            return mine.ToDtoList();
        }

        var cached = await cache.GetAsync<IEnumerable<ProjectDto>>(AllProjectsCacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Returning projects from cache");
            return cached;
        }

        var allProjects = await projectRepository.GetAllAsync();
        var dtos = allProjects.ToDtoList();
        await cache.SetAsync(AllProjectsCacheKey, dtos, TimeSpan.FromMinutes(5));

        return dtos;
    }

    public async Task<PagedResultDto<ProjectDto>> GetPagedProjectsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null)
    {
        logger.LogInformation(
            "Fetching projects page {Page} (size {PageSize}, search='{Search}') for user={UserId} agent={AgentId}",
            page, pageSize, search, forUserId, forAgentId);

        var (items, totalCount) = await projectRepository.GetPagedAsync(page, pageSize, search, forUserId, forAgentId);

        logger.LogInformation("Returned {Count}/{Total} projects for page {Page}", items.Count, totalCount, page);

        return new PagedResultDto<ProjectDto>
        {
            Items = items.ToDtoList().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int id)
    {
        logger.LogInformation("Fetching project {ProjectId}", id);

        var cacheKey = $"projects:{id}";
        var cached = await cache.GetAsync<ProjectDto>(cacheKey);
        if (cached is not null)
            return cached;

        var project = await projectRepository.GetByIdAsync(id);
        if (project is null)
            return null;

        var dto = project.ToDto();
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<Project> CreateProjectAsync(CreateProjectDto dto)
    {
        logger.LogInformation("Creating project {Name}", dto.Name);

        var nameTaken = await context.Projects.AnyAsync(p => p.Name == dto.Name);
        if (nameTaken)
            throw new ConflictException($"A project named '{dto.Name}' already exists.");

        var ownerExists = await context.Users.AnyAsync(u => u.Id == dto.OwnerUserId);
        if (!ownerExists)
            throw new NotFoundException("User", dto.OwnerUserId);

        if (dto.ProjectCategoryId.HasValue)
        {
            var categoryExists = await context.Set<ProjectCategory>().AnyAsync(c => c.Id == dto.ProjectCategoryId.Value);
            if (!categoryExists)
                throw new NotFoundException("ProjectCategory", dto.ProjectCategoryId.Value);
        }

        Team? team = null;
        if (dto.TeamId.HasValue)
        {
            team = await context.Set<Team>()
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == dto.TeamId.Value)
                ?? throw new NotFoundException("Team", dto.TeamId.Value);
        }

        var project = new Project(dto.Name, dto.Description, dto.OwnerUserId, dto.ProjectCategoryId, dto.IconKey)
        {
            AllowCustomerTicketCreation = dto.AllowCustomerTicketCreation,
            RequireApprovalForClose = dto.RequireApprovalForClose,
            NotificationEmail = dto.NotificationEmail
        };

        project.Members.Add(new ProjectMember { UserId = dto.OwnerUserId, Role = ProjectRole.Owner });

        if (team is not null)
        {
            project.ProjectTeams.Add(new ProjectTeam { Team = team });

            foreach (var teamMember in team.Members.Where(m => m.UserId.HasValue && m.UserId != dto.OwnerUserId))
            {
                project.Members.Add(new ProjectMember { UserId = teamMember.UserId, Role = ProjectRole.Member });
            }
        }

        var created = await projectRepository.CreateAsync(project);

        logger.LogInformation("Project {ProjectId} created successfully", created.Id);
        await cache.RemoveAsync(AllProjectsCacheKey);
        if (team is not null)
            await cache.RemoveAsync("teams:all");

        await hubContext.Clients
            .Group($"user-{dto.OwnerUserId}")
            .SendAsync("ReceiveNotification", new
            {
                message = $"Project '{created.Name}' created successfully.",
                type = "success"
            });

        return created;
    }

    public async Task<Project> CreateProjectFromTemplateAsync(CreateProjectFromTemplateDto dto)
    {
        logger.LogInformation("Creating project {Name} from template {TemplateId}", dto.Name, dto.TemplateId);

        var template = await context.Set<ProjectTemplate>().FindAsync(dto.TemplateId)
            ?? throw new NotFoundException("ProjectTemplate", dto.TemplateId);

        var createDto = new CreateProjectDto
        {
            Name = dto.Name,
            Description = template.Description,
            OwnerUserId = dto.OwnerUserId,
            ProjectCategoryId = template.DefaultProjectCategoryId,
            IconKey = template.DefaultIconKey,
            AllowCustomerTicketCreation = template.DefaultAllowCustomerTicketCreation,
            RequireApprovalForClose = template.DefaultRequireApprovalForClose
        };

        return await CreateProjectAsync(createDto);
    }

    public async Task ArchiveProjectAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        logger.LogInformation("Archiving project {ProjectId}", id);

        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(id, actingUserId, actingAgentId, ProjectRole.Owner);

        var project = await projectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Project", id);

        project.Archive();
        await projectRepository.UpdateAsync(project);

        await cache.RemoveAsync(AllProjectsCacheKey);
        await cache.RemoveAsync($"projects:{id}");

        foreach (var member in project.Members.Where(m => m.UserId.HasValue))
        {
            await hubContext.Clients
                .Group($"user-{member.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    message = $"Project '{project.Name}' has been archived.",
                    type = "info"
                });
        }
    }

    public async Task DeleteProjectAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        logger.LogInformation("Deleting project {ProjectId}", id);

        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(id, actingUserId, actingAgentId, ProjectRole.Owner);

        var exists = await context.Projects.AnyAsync(p => p.Id == id);
        if (!exists)
            throw new NotFoundException("Project", id);

        var hasActiveTickets = await context.Tickets.AnyAsync(t => t.ProjectId == id && !t.IsDeleted);
        if (hasActiveTickets)
            throw new ConflictException("Cannot delete project with active tickets.");

        await projectRepository.DeleteAsync(id);



        await cache.RemoveAsync(AllProjectsCacheKey);
        await cache.RemoveAsync($"projects:{id}");
    }

    public async Task UpdateSettingsAsync(int id, UpdateProjectSettingsDto dto, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        logger.LogInformation("Updating settings for project {ProjectId}", id);

        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(id, actingUserId, actingAgentId, ProjectRole.Owner);

        var project = await projectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Project", id);

        project.AllowCustomerTicketCreation = dto.AllowCustomerTicketCreation;
        project.RequireApprovalForClose = dto.RequireApprovalForClose;
        project.NotificationEmail = dto.NotificationEmail;

        await projectRepository.UpdateAsync(project);

        await cache.RemoveAsync(AllProjectsCacheKey);
        await cache.RemoveAsync($"projects:{id}");
    }
}
