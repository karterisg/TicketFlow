using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Services;

public class ProjectAccessService(AppDbContext context) : IProjectAccessService
{
    public async Task<ProjectRole?> GetMemberRoleAsync(int projectId, int? userId, int? agentId)
    {
        var member = await context.Set<ProjectMember>()
            .FirstOrDefaultAsync(m => m.ProjectId == projectId &&
                ((userId != null && m.UserId == userId) || (agentId != null && m.AgentId == agentId)));

        return member?.Role;
    }

    public async Task EnsureRoleAsync(int projectId, int? userId, int? agentId, ProjectRole minimumRole)
    {
        var role = await GetMemberRoleAsync(projectId, userId, agentId)
            ?? throw new ForbiddenException("You are not a member of this project.");

        if (role < minimumRole)
            throw new ForbiddenException($"Requires {minimumRole} role or higher.");
    }
}
