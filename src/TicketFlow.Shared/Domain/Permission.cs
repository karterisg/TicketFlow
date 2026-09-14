namespace TicketFlow.Shared.Domain;

public class Permission
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<UserPermission> UserPermissions { get; set; } = new();

    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
}
