namespace TicketFlow.Shared.DTOs;

public class TicketActivityDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}