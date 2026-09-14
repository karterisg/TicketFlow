namespace TicketFlow.Shared.Domain;

public class TimeEntry : BaseEntity
{
    public int TicketId { get; set; }
    public int AgentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? ManualMinutes { get; set; } //manual log
    public string? Note { get; set; }

    // Navigation properties
    public Agent Agent { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;

    // Computed — how much worked in minutes, either from manual log or calculated from start and end times
    public int TotalMinutes =>ManualMinutes ??(EndedAt.HasValue? (int)(EndedAt.Value - StartedAt).TotalMinutes : 0);
}