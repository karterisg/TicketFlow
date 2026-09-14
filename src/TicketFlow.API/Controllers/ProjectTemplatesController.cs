using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Services;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectTemplatesController(AppDbContext context, ILogger<ProjectTemplatesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await context.Set<ProjectTemplate>()
            .Select(t => new ProjectTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                DefaultProjectCategoryId = t.DefaultProjectCategoryId,
                DefaultIconKey = t.DefaultIconKey,
                DefaultAllowCustomerTicketCreation = t.DefaultAllowCustomerTicketCreation,
                DefaultRequireApprovalForClose = t.DefaultRequireApprovalForClose,
                Timeline = t.Timeline
            })
            .ToListAsync();
        return Ok(templates);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null)
    {
        logger.LogInformation("Fetching project templates page {Page} (size {PageSize}, search='{Search}')", page, pageSize, search);

        var query = context.Set<ProjectTemplate>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ProjectTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                DefaultProjectCategoryId = t.DefaultProjectCategoryId,
                DefaultIconKey = t.DefaultIconKey,
                DefaultAllowCustomerTicketCreation = t.DefaultAllowCustomerTicketCreation,
                DefaultRequireApprovalForClose = t.DefaultRequireApprovalForClose,
                Timeline = t.Timeline
            })
            .ToListAsync();

        logger.LogInformation("Returned {Count}/{Total} project templates for page {Page}", items.Count, totalCount, page);

        return Ok(new PagedResultDto<ProjectTemplateDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await context.Set<ProjectTemplate>()
            .Where(t => t.Id == id)
            .Select(t => new ProjectTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                DefaultProjectCategoryId = t.DefaultProjectCategoryId,
                DefaultIconKey = t.DefaultIconKey,
                DefaultAllowCustomerTicketCreation = t.DefaultAllowCustomerTicketCreation,
                DefaultRequireApprovalForClose = t.DefaultRequireApprovalForClose,
                Timeline = t.Timeline
            })
            .FirstOrDefaultAsync();
        return template is null ? NotFound() : Ok(template);
    }

    [Authorize(Policy = "Perm:ProjectTemplates.Manage")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectTemplateDto dto)
    {
        var template = new ProjectTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            DefaultProjectCategoryId = dto.DefaultProjectCategoryId,
            DefaultIconKey = dto.DefaultIconKey,
            DefaultAllowCustomerTicketCreation = dto.DefaultAllowCustomerTicketCreation,
            DefaultRequireApprovalForClose = dto.DefaultRequireApprovalForClose,
            Timeline = dto.Timeline
        };
        context.Set<ProjectTemplate>().Add(template);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [Authorize(Policy = "Perm:ProjectTemplates.Manage")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await context.Set<ProjectTemplate>().FindAsync(id);
        if (template is null) return NotFound();
        context.Set<ProjectTemplate>().Remove(template);
        await context.SaveChangesAsync();
        return NoContent();
    }




    [Authorize(Policy = "Perm:ProjectTemplates.Manage")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Modify(int id, [FromBody] UpdateProjectTemplateDto dto)
    {
        var template = await context.Set<ProjectTemplate>().FindAsync(id);

        if (template == null)
            return NotFound();

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.DefaultProjectCategoryId = dto.DefaultProjectCategoryId;   
        template.DefaultIconKey = dto.DefaultIconKey;
        template.DefaultAllowCustomerTicketCreation = dto.DefaultAllowCustomerTicketCreation;
        template.DefaultRequireApprovalForClose = dto.DefaultRequireApprovalForClose;
        template.Timeline = dto.Timeline;

        await context.SaveChangesAsync();

        return NoContent();
    }
}
