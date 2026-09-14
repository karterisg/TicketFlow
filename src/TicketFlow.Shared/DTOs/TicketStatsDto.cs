namespace TicketFlow.Shared.DTOs;

public class TicketStatsDto
{
    public int Total { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
}
