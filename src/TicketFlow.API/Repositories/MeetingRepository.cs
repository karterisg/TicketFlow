using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories;

public class MeetingRepository(AppDbContext context) : IMeetingRepository
{
    public async Task<IEnumerable<Meeting>> GetAllAsync(int? projectId, int? teamId, CancellationToken cancellationToken = default)
    {
        var query = context.Meetings
            .AsNoTracking()
            .Include(m => m.Project)
            .Include(m => m.Team)
            .Include(m => m.Attendees)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(m => m.ProjectId == projectId.Value);

        if (teamId.HasValue)
            query = query.Where(m => m.TeamId == teamId.Value);

        return await query
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Meeting> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default)
    {
        var query = context.Meetings
            .AsNoTracking()
            .Include(m => m.Project)
            .Include(m => m.Team)
            .Include(m => m.Attendees)
            .AsQueryable();

        if (forUserId.HasValue)
        {
            var projectIds = context.Set<ProjectMember>().Where(pm => pm.UserId == forUserId.Value).Select(pm => pm.ProjectId);
            var teamIds = context.Set<TeamMember>().Where(tm => tm.UserId == forUserId.Value).Select(tm => tm.TeamId);
            query = query.Where(m =>
                (m.ProjectId.HasValue && projectIds.Contains(m.ProjectId.Value)) ||
                (m.TeamId.HasValue && teamIds.Contains(m.TeamId.Value)) ||
                m.CreatedByUserId == forUserId.Value);
        }
        else if (forAgentId.HasValue)
        {
            var projectIds = context.Set<ProjectMember>().Where(pm => pm.AgentId == forAgentId.Value).Select(pm => pm.ProjectId);
            var teamIds = context.Set<TeamMember>().Where(tm => tm.AgentId == forAgentId.Value).Select(tm => tm.TeamId);
            query = query.Where(m =>
                (m.ProjectId.HasValue && projectIds.Contains(m.ProjectId.Value)) ||
                (m.TeamId.HasValue && teamIds.Contains(m.TeamId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Meeting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Meetings
            .AsNoTracking()
            .Include(m => m.Project)
            .Include(m => m.Team)
            .Include(m => m.Attendees).ThenInclude(a => a.User)
            .Include(m => m.Attendees).ThenInclude(a => a.Agent)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Meeting> CreateAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync(cancellationToken);
        return meeting;
    }

    public async Task UpdateAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        meeting.UpdatedAt = DateTime.UtcNow;
        context.Meetings.Update(meeting);
        await context.SaveChangesAsync(cancellationToken);
    }
}