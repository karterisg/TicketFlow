using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? forUserId, [FromQuery] int? forAgentId)
    {
        var projects = await projectService.GetAllProjectsAsync(forUserId, forAgentId);
        return Ok(projects);
    }



    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null,
        [FromQuery] int? forUserId = null, [FromQuery] int? forAgentId = null)
    {
        var result = await projectService.GetPagedProjectsAsync(page, pageSize, search, forUserId, forAgentId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await projectService.GetProjectByIdAsync(id);
        return project is null ? NotFound() : Ok(project);
    }

    [Authorize(Policy = "Perm:Projects.Manage")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        var project = await projectService.CreateProjectAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToDto());
    }

    [Authorize(Policy = "Perm:Projects.Manage")]
    [HttpPost("from-template")]
    public async Task<IActionResult> CreateFromTemplate([FromBody] CreateProjectFromTemplateDto dto)
    {
        var project = await projectService.CreateProjectFromTemplateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToDto());
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        await projectService.ArchiveProjectAsync(id, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }

    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateSettings(int id, [FromBody] UpdateProjectSettingsDto dto)
    {
        await projectService.UpdateSettingsAsync(id, dto, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }

    [Authorize(Policy = "Perm:Projects.Manage")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await projectService.DeleteProjectAsync(id, User.GetUserId(), User.GetAgentId(), User.IsInRole("Manager"));
        return NoContent();
    }
}
