using System.Security.Cryptography.X509Certificates;

namespace TicketFlow.Shared.DTOs;
public class TimeEntryDto
{
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? Note { get; set; }
        public int TotalMinutes { get; set; } 
        // Computed — how much worked in minutes, either from manual log or calculated from start and end times
        public bool IsRunning => EndedAt is null && TotalMinutes == 0;
}

public class LogTimeDto
{
    //public int Agent { get; set; }
    public int AgentId { get; set; }
    public int Minutes { get; set; }
    public string? Note { get; set; }
}

public class StartTimerDto
{
    public int AgentId { get; set; }
}

public class StopTimerDto
{
    public int AgentId { get; set; }
}
