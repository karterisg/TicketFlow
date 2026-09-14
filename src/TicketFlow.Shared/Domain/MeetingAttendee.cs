namespace TicketFlow.Shared.Domain;

public class MeetingAttendee : BaseEntity
{
    public int MeetingId { get; set; }
    public int? UserId { get; set; }
    public int? AgentId { get; set; }
    public AttendeeResponse Response { get; set; } = AttendeeResponse.Pending;

    public Meeting Meeting { get; set; } = null!;
    public User? User { get; set; }
    public Agent? Agent { get; set; }
}
