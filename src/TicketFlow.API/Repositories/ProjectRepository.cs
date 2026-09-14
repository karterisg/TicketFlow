using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Repositories;

public class ProjectRepository(AppDbContext context) : IProjectRepository
{
    public async Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectCategory)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Project> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? forUserId, int? forAgentId, CancellationToken cancellationToken = default)
    {
        var query = context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectCategory)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .AsQueryable();

        if (forUserId.HasValue)
            query = query.Where(p => p.OwnerUserId == forUserId.Value || p.Members.Any(m => m.UserId == forUserId.Value));

        if (forAgentId.HasValue)
            query = query.Where(p => p.Members.Any(m => m.AgentId == forAgentId.Value));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectCategory)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Projects.Add(project);
        await context.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        project.UpdatedAt = DateTime.UtcNow;
        context.Projects.Update(project);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is not null)
        {
            project.Delete();
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
