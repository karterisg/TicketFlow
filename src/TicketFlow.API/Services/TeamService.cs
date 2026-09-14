using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Services;

public class TeamService(
    ITeamRepository teamRepository,
    AppDbContext context,
    ICacheService cache,
    ILogger<TeamService> logger,
    ITeamAccessService accessService) : ITeamService
{
    private const string AllTeamsCacheKey = "teams:all";

    public async Task<IEnumerable<TeamDto>> GetAllTeamsAsync(int? forUserId = null, int? forAgentId = null)
    {
        if (forUserId.HasValue)
        {
            var teams = await teamRepository.GetAllAsync();
            var mine = teams.Where(t => t.OwnerUserId == forUserId.Value
                || t.Members.Any(m => m.UserId == forUserId.Value));
            return mine.ToDtoList();
        }

        if (forAgentId.HasValue)
        {
            var teams = await teamRepository.GetAllAsync();
            var mine = teams.Where(t => t.Members.Any(m => m.AgentId == forAgentId.Value));
            return mine.ToDtoList();
        }

        var cached = await cache.GetAsync<IEnumerable<TeamDto>>(AllTeamsCacheKey);
        if (cached is not null)
            return cached;


        var allTeams = await teamRepository.GetAllAsync();
        var dtos = allTeams.ToDtoList();
        await cache.SetAsync(AllTeamsCacheKey, dtos, TimeSpan.FromMinutes(5));

        return dtos;
    }

    public async Task<PagedResultDto<TeamDto>> GetPagedTeamsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null)
    {
        logger.LogInformation(
            "Fetching teams page {Page} (size {PageSize}, search='{Search}') for user={UserId} agent={AgentId}",
            page, pageSize, search, forUserId, forAgentId);

        var (items, totalCount) = await teamRepository.GetPagedAsync(page, pageSize, search, forUserId, forAgentId);

        logger.LogInformation("Returned {Count}/{Total} teams for page {Page}", items.Count, totalCount, page);

        return new PagedResultDto<TeamDto>
        {
            Items = items.ToDtoList().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TeamDto?> GetTeamByIdAsync(int id)
    {
        var team = await teamRepository.GetByIdAsync(id);
        return team?.ToDto();
    }

    public async Task<Team> CreateTeamAsync(CreateTeamDto dto)
    {
        logger.LogInformation("Creating team {Name}", dto.Name);

        var ownerExists = await context.Users.AnyAsync(u => u.Id == dto.OwnerUserId);
        if (!ownerExists)
            throw new NotFoundException("User", dto.OwnerUserId);

        var team = new Team(dto.Name, dto.Description, dto.OwnerUserId);
        team.Members.Add(new TeamMember { UserId = dto.OwnerUserId, Role = TeamRole.Lead });

        var created = await teamRepository.CreateAsync(team);

        logger.LogInformation("Team {TeamId} created successfully", created.Id);
        await cache.RemoveAsync(AllTeamsCacheKey);

        return created;
    }

    public async Task DeleteTeamAsync(int id, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(id, actingUserId, actingAgentId, TeamRole.Lead);

        var exists = await context.Set<Team>().AnyAsync(t => t.Id == id);
        if (!exists)
            throw new NotFoundException("Team", id);

        await teamRepository.DeleteAsync(id);

        await cache.RemoveAsync(AllTeamsCacheKey);
    }

    public async Task AssignTeamToProjectAsync(int projectId, int teamId, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(teamId, actingUserId, actingAgentId, TeamRole.Lead);

        var projectExists = await context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new NotFoundException("Project", projectId);

        var teamExists = await context.Set<Team>().AnyAsync(t => t.Id == teamId);
        if (!teamExists)
            throw new NotFoundException("Team", teamId);

        var alreadyAssigned = await context.Set<ProjectTeam>()
            .AnyAsync(pt => pt.ProjectId == projectId && pt.TeamId == teamId);
        if (alreadyAssigned)
            throw new ConflictException("This team is already assigned to the project.");

        context.Set<ProjectTeam>().Add(new ProjectTeam { ProjectId = projectId, TeamId = teamId });
        await context.SaveChangesAsync();

        await cache.RemoveAsync(AllTeamsCacheKey);
        await cache.RemoveAsync("projects:all");
    }

    public async Task UnassignTeamFromProjectAsync(int projectId, int teamId, int? actingUserId, int? actingAgentId, bool isGlobalManager)
    {
        if (!isGlobalManager)
            await accessService.EnsureRoleAsync(teamId, actingUserId, actingAgentId, TeamRole.Lead);

        var link = await context.Set<ProjectTeam>()
            .FirstOrDefaultAsync(pt => pt.ProjectId == projectId && pt.TeamId == teamId)
            ?? throw new NotFoundException("ProjectTeam assignment", teamId);

        context.Set<ProjectTeam>().Remove(link);
        await context.SaveChangesAsync();

        await cache.RemoveAsync(AllTeamsCacheKey);
    }
}
