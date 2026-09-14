using Microsoft.VisualBasic;

namespace TicketFlow.Shared.Domain;

public class Ticket : BaseEntity, ISoftDeletable, IAuditable
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public DateTime? ResolvedAt { get; set; }

    public int UserId { get; set; }
    public int? AgentId { get; private set; }
    public int CategoryId { get; set; }

    //added the project id to the ticket class to link it to the project (optional - a ticket may not belong to any project)
    public int? ProjectId { get; set; }


    public int TicketNumber { get; set; } // sequential ticketing, ignores soft-deleted (for now we do it for notification system)



    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public Agent? Agent { get; private set; }
    public Category Category { get; set; } = null!;
    public Project? Project { get; set; }
    public List<Comment> Comments { get; set; } = new();


    public DateTime? DueDate { get; set; }


    public List<TimeEntry> TimeEntries { get; set; } = new();



    // Parameterless constructor for EF Core
    public Ticket() { }

    public Ticket(string title, string description, int userId, int categoryId, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title;
        Description = description;
        UserId = userId;
        CategoryId = categoryId;
        DueDate = dueDate;
    }

    public void Assign(Agent agent)
    {
        if (Status == TicketStatus.InProgress)
            throw new InvalidOperationException("Ticket is already assigned.");

        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot assign a closed ticket.");

        AgentId = agent.Id;
        Agent = agent;
        Status = TicketStatus.InProgress;

        UpdatedAt = DateTime.UtcNow;
    }


   


    public void Resolve()
    {
        if (Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Only in-progress tickets can be resolved.");

        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("Ticket is already closed.");

        Status = TicketStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}