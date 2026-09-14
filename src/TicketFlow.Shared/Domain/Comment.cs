namespace TicketFlow.Shared.Domain;

public class Comment : BaseEntity
{
    public string Body { get; set; } = string.Empty;

    public int TicketId { get; set; }
    public int AuthorId { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public User Author { get; set; } = null!; //it is used by the TicketRepository to include the Author when fetching a ticket with its comments
}