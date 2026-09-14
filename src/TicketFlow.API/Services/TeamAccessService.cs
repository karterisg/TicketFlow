using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Services;

public class TeamAccessService(AppDbContext context) : ITeamAccessService
{
    public async Task<TeamRole?> GetMemberRoleAsync(int teamId, int? userId, int? agentId)
    {
        var member = await context.Set<TeamMember>()
            .FirstOrDefaultAsync(m => m.TeamId == teamId &&
                ((userId != null && m.UserId == userId) || (agentId != null && m.AgentId == agentId)));

        return member?.Role;
    }

    public async Task<bool> IsMemberAsync(int teamId, int? userId, int? agentId)
    {
        var role = await GetMemberRoleAsync(teamId, userId, agentId);
        return role.HasValue;
    }


    public async Task EnsureRoleAsync(int teamId, int? userId, int? agentId, TeamRole minimumRole)
    {
        var role = await GetMemberRoleAsync(teamId, userId, agentId)
            ?? throw new ForbiddenException("You are not a member of this team.");

        if (role < minimumRole)
            throw new ForbiddenException($"Requires {minimumRole} role or higher.");
    }

    //same as ensure role but returns the role instead of throwing an exception
    //public async Task GetMemberRoleByIdAsync(int teamId, int userId)
    //{
    //    var role = await GetMemberRoleAsync(teamId, userId)
    //        ?? throw new ForbiddenException("You are not a member of this team."); return;
    //}
}
