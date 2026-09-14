namespace TicketFlow.Shared.DTOs;

public class AgentDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int AssignedTicketCount { get; set; }
}

public class CreateAgentDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}