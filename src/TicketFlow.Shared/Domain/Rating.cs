namespace TicketFlow.Shared.Domain;

public class Rating : BaseEntity
{
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public User User { get; set; } = null!;

    public Rating() { }

    public Rating(int ticketId, int userId, int score, string? comment)
    {
        TicketId = ticketId;
        UserId = userId;
        Score = score;
        Comment = comment;
    }
}
