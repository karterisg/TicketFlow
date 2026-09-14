using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/teams/{teamId}/members")]
public class TeamMembersController(AppDbContext context, ITeamAccessService accessService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int teamId)
    {
        var members = await context.Set<TeamMember>()
            .Include(m => m.User)
            .Include(m => m.Agent)
            .Where(m => m.TeamId == teamId)
            .Select(m => new TeamMemberDto
            {
                Id = m.Id,
                TeamId = m.TeamId,
                UserId = m.UserId,
                AgentId = m.AgentId,
                MemberName = m.User != null ? m.User.FullName : (m.Agent != null ? m.Agent.FullName : string.Empty),
                Role = m.Role.ToString()
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int teamId, [FromBody] AddTeamMemberDto dto)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(teamId, User.GetUserId(), User.GetAgentId(), TeamRole.Lead);

        if (!Enum.TryParse<TeamRole>(dto.Role, true, out var role))
            return BadRequest("Invalid role.");

        var alreadyMember = await context.Set<TeamMember>().AnyAsync(m =>
            m.TeamId == teamId &&
            ((dto.UserId != null && m.UserId == dto.UserId) ||
             (dto.AgentId != null && m.AgentId == dto.AgentId)));
        if (alreadyMember)
            return Conflict("This member is already part of the team.");

        var member = new TeamMember
        {
            TeamId = teamId,
            UserId = dto.UserId,
            AgentId = dto.AgentId,
            Role = role
        };

        context.Set<TeamMember>().Add(member);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { teamId }, member);
    }

    [HttpPut("{memberId}/role")]
    public async Task<IActionResult> ChangeRole(int teamId, int memberId, [FromBody] ChangeTeamMemberRoleDto dto)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(teamId, User.GetUserId(), User.GetAgentId(), TeamRole.Lead);

        var member = await context.Set<TeamMember>()
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TeamId == teamId);
        if (member is null) return NotFound();

        if (!Enum.TryParse<TeamRole>(dto.Role, true, out var role))
            return BadRequest("Invalid role.");

        member.Role = role;
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{memberId}")]
    public async Task<IActionResult> Remove(int teamId, int memberId)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(teamId, User.GetUserId(), User.GetAgentId(), TeamRole.Lead);

        var member = await context.Set<TeamMember>()
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TeamId == teamId);
        if (member is null) return NotFound();

        context.Set<TeamMember>().Remove(member);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
