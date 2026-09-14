namespace TicketFlow.Shared.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
