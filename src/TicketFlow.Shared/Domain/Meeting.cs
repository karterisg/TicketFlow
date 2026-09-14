namespace TicketFlow.Shared.Domain;

public class Meeting : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ProjectId { get; set; }
    public int? TeamId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? LocationOrLink { get; set; }
    public MeetingStatus Status { get; private set; } = MeetingStatus.Scheduled;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public Project? Project { get; set; }
    public Team? Team { get; set; }
    public List<MeetingAttendee> Attendees { get; set; } = new();

    public Meeting() { }

    public Meeting(string title, string? description, int createdByUserId, DateTime scheduledAt, int durationMinutes, int? projectId, int? teamId, string? locationOrLink)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title;
        Description = description;
        CreatedByUserId = createdByUserId;
        ScheduledAt = scheduledAt;
        DurationMinutes = durationMinutes;
        ProjectId = projectId;
        TeamId = teamId;
        LocationOrLink = locationOrLink;
    }

    public void Cancel()
    {
        if (Status == MeetingStatus.Cancelled)
            throw new InvalidOperationException("Meeting is already cancelled.");

        Status = MeetingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != MeetingStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled meetings can be completed.");

        Status = MeetingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
