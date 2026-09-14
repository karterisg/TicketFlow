using System.Security.Claims;

namespace TicketFlow.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("domainUserId");
        return int.TryParse(value, out var id) ? id : null;
    }

    public static int? GetAgentId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("agentId");
        return int.TryParse(value, out var id) ? id : null;
    }

    public static int RequireUserId(this ClaimsPrincipal user) =>
        user.GetUserId() ?? throw new UnauthorizedAccessException("Token has no domain user id.");

    public static int RequireAgentId(this ClaimsPrincipal user) =>
        user.GetAgentId() ?? throw new UnauthorizedAccessException("Token has no agent id.");
}
