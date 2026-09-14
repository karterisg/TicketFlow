namespace TicketFlow.Shared.DTOs;

public class CommentDto
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
}

public class CreateCommentDto
{
    public string Body { get; set; } = string.Empty;
    public int AuthorId { get; set; }
}