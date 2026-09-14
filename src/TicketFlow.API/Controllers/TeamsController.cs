using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TeamsController(ITeamService teamService, AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? forUserId, [FromQuery] int? forAgentId)
    {
        var teams = await teamService.GetAllTeamsAsync(forUserId, forAgentId);
        return Ok(teams);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null,
        [FromQuery] int? forUserId = null, [FromQuery] int? forAgentId = null)
    {
        var result = await teamService.GetPagedTeamsAsync(page, pageSize, search, forUserId, forAgentId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var team = await teamService.GetTeamByIdAsync(id);
        return team is null ? NotFound() : Ok(team);
    }

    [Authorize(Policy = "Perm:Teams.Manage")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
    {
        var team = await teamService.CreateTeamAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team.ToDto());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await teamService.DeleteTeamAsync(id, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }



    [HttpGet("{teamId}/projects")]
    public async Task<IActionResult> GetProjects(int teamId)
    {
        var links = await context.Set<ProjectTeam>()
            .Include(pt => pt.Project)
            .Where(pt => pt.TeamId == teamId)
            .Select(pt => new ProjectTeamDto
            {
                Id = pt.Id,
                ProjectId = pt.ProjectId,
                ProjectName = pt.Project.Name,
                TeamId = pt.TeamId,
                TeamName = pt.Team.Name
            })
            .ToListAsync();

        return Ok(links);
    }

    [HttpPost("{teamId}/projects/{projectId}")]
    public async Task<IActionResult> AssignToProject(int teamId, int projectId)
    {
        await teamService.AssignTeamToProjectAsync(projectId, teamId, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }

    [HttpDelete("{teamId}/projects/{projectId}")]
    public async Task<IActionResult> UnassignFromProject(int teamId, int projectId)
    {
        await teamService.UnassignTeamFromProjectAsync(projectId, teamId, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }
}
