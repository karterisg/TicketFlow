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
[Route("api/projects/{projectId}/members")]
public class ProjectMembersController(AppDbContext context, IProjectAccessService accessService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int projectId)
    {
        var members = await context.Set<ProjectMember>()
            .Include(m => m.User)
            .Include(m => m.Agent)
            .Where(m => m.ProjectId == projectId)
            .Select(m => new ProjectMemberDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                UserId = m.UserId,
                AgentId = m.AgentId,
                MemberName = m.User != null ? m.User.FullName : (m.Agent != null ? m.Agent.FullName : string.Empty),
                Role = m.Role.ToString()
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int projectId, [FromBody] AddProjectMemberDto dto)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(projectId, User.GetUserId(), User.GetAgentId(), ProjectRole.Owner);

        if (!Enum.TryParse<ProjectRole>(dto.Role, true, out var role))
            return BadRequest("Invalid role.");

        var alreadyMember = await context.Set<ProjectMember>().AnyAsync(m =>
            m.ProjectId == projectId &&
            ((dto.UserId != null && m.UserId == dto.UserId) ||
             (dto.AgentId != null && m.AgentId == dto.AgentId)));
        if (alreadyMember)
            return Conflict("This member is already part of the project.");

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = dto.UserId,
            AgentId = dto.AgentId,
            Role = role
        };

        context.Set<ProjectMember>().Add(member);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { projectId }, member);
    }

    [HttpPut("{memberId}/role")]
    public async Task<IActionResult> ChangeRole(int projectId, int memberId, [FromBody] ChangeProjectMemberRoleDto dto)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(projectId, User.GetUserId(), User.GetAgentId(), ProjectRole.Owner);

        var member = await context.Set<ProjectMember>()
            .FirstOrDefaultAsync(m => m.Id == memberId && m.ProjectId == projectId);
        if (member is null) return NotFound();

        if (!Enum.TryParse<ProjectRole>(dto.Role, true, out var role))
            return BadRequest("Invalid role.");

        member.Role = role;
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{memberId}")]
    public async Task<IActionResult> Remove(int projectId, int memberId)
    {
        if (!User.IsInRole("Manager"))
            await accessService.EnsureRoleAsync(projectId, User.GetUserId(), User.GetAgentId(), ProjectRole.Owner);

        var member = await context.Set<ProjectMember>()
            .FirstOrDefaultAsync(m => m.Id == memberId && m.ProjectId == projectId);
        if (member is null) return NotFound();

        context.Set<ProjectMember>().Remove(member);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
