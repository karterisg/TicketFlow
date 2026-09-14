namespace TicketFlow.Shared.Domain;

public class TicketActivity : BaseEntity
{
    public int TicketId { get; set; }
    public TicketActivityType ActionType { get; set; }
    public string? Description { get; set; }
    public int? ActorId { get; set; }
    public string? ActorName { get; set; } = "System"; 

    public Ticket Ticket { get; set; } = null;
    public TicketActivity() {}
    public TicketActivity(int ticketId, TicketActivityType actionType, string description, int? actorId, string actorName)
    {
        TicketId = ticketId;
        ActionType = actionType;
        Description = description;
        ActorId = actorId;
        ActorName = actorName;
    }
}