using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.Key)
            .Select(p => new PermissionDto { Id = p.Id, Key = p.Key, Category = p.Category, Description = p.Description })
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var applicationUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(applicationUserId))
            return Unauthorized();

        var keys = await context.UserPermissions
            .AsNoTracking()
            .Where(up => up.ApplicationUserId == applicationUserId)
            .Select(up => up.Permission.Key)
            .ToListAsync();

        return Ok(keys);
    }

    [HttpGet("user/{applicationUserId}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetForUser(string applicationUserId)
    {
        var keys = await context.UserPermissions
            .AsNoTracking()
            .Where(up => up.ApplicationUserId == applicationUserId)
            .Select(up => up.Permission.Key)
            .ToListAsync();

        return Ok(keys);
    }

    [HttpPut("user/{applicationUserId}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateForUser(string applicationUserId, [FromBody] UpdateUserPermissionsDto dto)
    {
        var existing = await context.UserPermissions
            .Where(up => up.ApplicationUserId == applicationUserId)
            .ToListAsync();
        context.UserPermissions.RemoveRange(existing);

        var validKeys = await context.Permissions
            .Where(p => dto.GrantedKeys.Contains(p.Key))
            .Select(p => new { p.Id })
            .ToListAsync();

        foreach (var permission in validKeys)
        {
            context.UserPermissions.Add(new UserPermission
            {
                ApplicationUserId = applicationUserId,
                PermissionId = permission.Id
            });
        }

        await context.SaveChangesAsync();
        return NoContent();
    }
}
