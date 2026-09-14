namespace TicketFlow.Shared.DTOs;

public class SubmitRatingDto
{
    public int Score { get; set; }
    public string? Comment { get; set; }
}

public class RatingDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
}
