namespace TicketFlow.Shared.Domain;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; set; }
    public int? UserId { get; set; }
    public int? AgentId { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Member;

    public Project Project { get; set; } = null!;
    public User? User { get; set; }
    public Agent? Agent { get; set; }
}
