using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories;

public class TeamRepository(AppDbContext context) : ITeamRepository
{
    //reads going to be put in different cqrs /queries
    public async Task<IEnumerable<Team>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Teams
            .AsNoTracking()
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.ProjectTeams)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Team> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default)
    {
        var query = context.Teams
            .AsNoTracking()
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.ProjectTeams)
            .AsQueryable();

        if (forUserId.HasValue)
            query = query.Where(t => t.OwnerUserId == forUserId.Value || t.Members.Any(m => m.UserId == forUserId.Value));

        if (forAgentId.HasValue)
            query = query.Where(t => t.Members.Any(m => m.AgentId == forAgentId.Value));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Team?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Teams
            .AsNoTracking()
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.Members).ThenInclude(m => m.Agent)
            .Include(t => t.ProjectTeams)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }




    //writes going to beput in different cqrs/commands

    public async Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default)
    {
        context.Teams.Add(team);
        await context.SaveChangesAsync(cancellationToken);
        return team;
    }

    public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        team.UpdatedAt = DateTime.UtcNow;
        context.Teams.Update(team);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var team = await context.Teams.FindAsync(new object[] { id }, cancellationToken);
        if (team is not null)
        {
            team.Delete();
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Team>> GetTeamsByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await context.ProjectTeams
            .AsNoTracking()
            .Where(pt => pt.ProjectId == projectId)
            .Select(pt => pt.Team)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.ProjectTeams)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetMemberRoleByIdAsync(int teamId, int userId, CancellationToken cancellationToken = default)
    {
        return await context.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.TeamId == teamId && tm.UserId == userId)
            .Select(tm => tm.Team)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .Include(t => t.ProjectTeams)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    }

}
