using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectCategoriesController(AppDbContext context, ILogger<ProjectCategoriesController> logger) : ControllerBase 
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await context.Set<ProjectCategory>()
            .Select(c => new ProjectCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProjectCount = c.Projects.Count
            })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null)
    {
        logger.LogInformation("Fetching project categories page {Page} (size {PageSize}, search='{Search}')", page, pageSize, search);

        var query = context.Set<ProjectCategory>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ProjectCategoryDto { Id = c.Id, Name = c.Name, ProjectCount = c.Projects.Count })
            .ToListAsync();

        logger.LogInformation("Returned {Count}/{Total} project categories for page {Page}", items.Count, totalCount, page);

        return Ok(new PagedResultDto<ProjectCategoryDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await context.Set<ProjectCategory>()
            .Where(c => c.Id == id)
            .Select(c => new ProjectCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProjectCount = c.Projects.Count
            })
            .FirstOrDefaultAsync();
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = "Perm:ProjectCategories.Manage")]
    public async Task<IActionResult> Create([FromBody] CreateProjectCategoryDto dto)
    {
        var category = new ProjectCategory { Name = dto.Name };
        context.Set<ProjectCategory>().Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Perm:ProjectCategories.Manage")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Set<ProjectCategory>().FindAsync(id);
        if (category is null) return NotFound();
        context.Set<ProjectCategory>().Remove(category);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Perm:ProjectCategories.Manage")]
    public async Task<IActionResult> Modify(int id, [FromBody] UpdateProjectCategoryDto dto)
    {
        var category = await context.Set<ProjectCategory>().FindAsync(id);
        if (category is null) return NotFound();

        category.Name = dto.Name;
        await context.SaveChangesAsync();
        return Ok(category);
    }
}
