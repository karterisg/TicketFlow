namespace TicketFlow.Shared.DTOs;

public class PermissionDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateUserPermissionsDto
{
    public List<string> GrantedKeys { get; set; } = new();
}
