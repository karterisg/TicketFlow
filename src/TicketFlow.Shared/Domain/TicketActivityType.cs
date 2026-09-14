namespace TicketFlow.Shared.Domain;

public enum TicketActivityType
{
    Created,
    StatusChanged,
    Assigned,
    PriorityChanged,
    CommentAdded,
    Resolved,
    Closed,
    Deleted,
    DueDateChanged,
    RatingSubmitted
}
