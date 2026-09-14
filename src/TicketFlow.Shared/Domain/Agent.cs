namespace TicketFlow.Shared.Domain;

public class Agent : Person
{
    public bool IsAvailable { get; set; } = true;

    public List<Ticket> AssignedTickets { get; set; } = new();

    public override string GetDisplayName() =>
        IsAvailable ? $"{FullName} ✓" : $"{FullName} (busy)";

    public List<TimeEntry> TimeEntries { get; set; } = new();

    //public bool IsOnline { get; set; } = false;
}    