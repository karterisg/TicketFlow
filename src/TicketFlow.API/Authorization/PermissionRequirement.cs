using Microsoft.AspNetCore.Authorization;

namespace TicketFlow.API.Authorization;

public class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}
