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
public class CategoriesController(AppDbContext context, ILogger<CategoriesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await context.Categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                TicketCount = c.Tickets.Count
            })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null)
    {
        logger.LogInformation("Fetching categories page {Page} (size {PageSize}, search='{Search}')", page, pageSize, search);

        var query = context.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                TicketCount = c.Tickets.Count
            })
            .ToListAsync();

        logger.LogInformation("Returned {Count}/{Total} categories for page {Page}", items.Count, totalCount, page);

        return Ok(new PagedResultDto<CategoryDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await context.Categories.FindAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = "Perm:Categories.Manage")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Perm:Categories.Manage")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound();
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Perm:Categories.Manage")]
    public async Task<IActionResult> Modify(int id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = dto.Name;
        await context.SaveChangesAsync();
        return Ok(category);
    }
}