namespace TicketFlow.Shared.Domain;

public class Team : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OwnerUserId { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public List<TeamMember> Members { get; set; } = new();
    public List<ProjectTeam> ProjectTeams { get; set; } = new();

    public Team() { }

    public Team(string name, string? description, int ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        OwnerUserId = ownerUserId;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
