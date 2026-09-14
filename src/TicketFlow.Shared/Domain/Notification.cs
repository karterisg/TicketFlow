namespace TicketFlow.Shared.Domain;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public DateTime? ReadAt { get; set; }

    public User User { get; set; } = null!;

    public Notification() { }

    public Notification(int userId, string message, string type)
    {
        UserId = userId;
        Message = message;
        Type = type;
    }
}
