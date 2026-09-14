namespace TicketFlow.Shared.Domain;

public class UserPermission : BaseEntity
{
    public string ApplicationUserId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public string Role { get; set; } = string.Empty; 
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
}
