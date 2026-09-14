namespace TicketFlow.Shared.Domain;

public class ProjectTeam : BaseEntity
{
    public int ProjectId { get; set; }
    public int TeamId { get; set; }

    public Project Project { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
