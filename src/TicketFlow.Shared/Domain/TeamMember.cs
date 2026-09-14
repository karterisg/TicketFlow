namespace TicketFlow.Shared.Domain;

public class TeamMember : BaseEntity
{
    public int TeamId { get; set; }
    public int? UserId { get; set; }
    public int? AgentId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;

    public Team Team { get; set; } = null!;
    public User? User { get; set; }
    public Agent? Agent { get; set; }
}
