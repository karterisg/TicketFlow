using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using System.Security.Claims;

namespace TicketFlow.API.Authorization;

public class PermissionAuthorizationHandler(AppDbContext context) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context_, PermissionRequirement requirement)
    {
        if (context_.User.IsInRole("Manager"))
        {
            context_.Succeed(requirement);
            return;
        }

        var applicationUserId = context_.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(applicationUserId))
            return;

        var hasPermission = await context.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.ApplicationUserId == applicationUserId && up.Permission.Key == requirement.PermissionKey);

        if (hasPermission)
            context_.Succeed(requirement);
    }
}
