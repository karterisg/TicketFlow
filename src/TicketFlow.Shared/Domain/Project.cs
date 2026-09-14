namespace TicketFlow.Shared.Domain;

public class Project : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Active;
    public string IconKey { get; set; } = "folder";
    public int? ProjectCategoryId { get; set; }
    public int OwnerUserId { get; set; }

    public bool AllowCustomerTicketCreation { get; set; } = true;
    public bool RequireApprovalForClose { get; set; } = false;
    public string? NotificationEmail { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public ProjectCategory? ProjectCategory { get; set; }
    public List<ProjectMember> Members { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
    public List<ProjectTeam> ProjectTeams { get; set; } = new();

    public Project() { }

    public Project(string name, string? description, int ownerUserId, int? projectCategoryId, string iconKey)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        OwnerUserId = ownerUserId;
        ProjectCategoryId = projectCategoryId;
        IconKey = iconKey;
    }

    public void Archive()
    {
        if (Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Project is already archived.");

        Status = ProjectStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status == ProjectStatus.Active)
            throw new InvalidOperationException("Project is already active.");

        Status = ProjectStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
