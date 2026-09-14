namespace TicketFlow.Shared.DTOs
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerUserId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int ProjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerUserId { get; set; }
    }

    public class TeamMemberDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AddTeamMemberDto
    {
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string Role { get; set; } = "Member";
    }

    public class ChangeTeamMemberRoleDto
    {
        public string Role { get; set; } = "Member";
    }

    public class AssignTeamToProjectDto
    {
        public int TeamId { get; set; }
    }

    public class ProjectTeamDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }
}
