namespace TicketFlow.Shared.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public int? ProjectCategoryId { get; set; }
        public string? ProjectCategoryName { get; set; }
        public int OwnerUserId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public bool AllowCustomerTicketCreation { get; set; }
        public bool RequireApprovalForClose { get; set; }
        public string? NotificationEmail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerUserId { get; set; }
        public int? ProjectCategoryId { get; set; }
        public int? TeamId { get; set; }
        public string IconKey { get; set; } = "folder";
        public bool AllowCustomerTicketCreation { get; set; } = true;
        public bool RequireApprovalForClose { get; set; } = false;
        public string? NotificationEmail { get; set; }
    }

    public class CreateProjectFromTemplateDto
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OwnerUserId { get; set; }
    }

    public class UpdateProjectSettingsDto
    {
        public bool AllowCustomerTicketCreation { get; set; }
        public bool RequireApprovalForClose { get; set; }
        public string? NotificationEmail { get; set; }
    }

    public class AddProjectMemberDto
    {
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string Role { get; set; } = "Member";
    }

    public class ChangeProjectMemberRoleDto
    {
        public string Role { get; set; } = "Member";
    }

    public class ProjectMemberDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ProjectCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProjectCount { get; set; }
    }

    public class CreateProjectCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateProjectCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ProjectTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DefaultProjectCategoryId { get; set; }
        public string DefaultIconKey { get; set; } = string.Empty;
        public bool DefaultAllowCustomerTicketCreation { get; set; }
        public bool DefaultRequireApprovalForClose { get; set; }
        public string Timeline { get; set; } = string.Empty;
    }

    public class CreateProjectTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DefaultProjectCategoryId { get; set; }
        public string DefaultIconKey { get; set; } = "folder";
        public bool DefaultAllowCustomerTicketCreation { get; set; } = true;
        public bool DefaultRequireApprovalForClose { get; set; } = false;
        public string Timeline { get; set; } = "default";
    }


    public class UpdateProjectTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DefaultProjectCategoryId { get; set; }
        public string DefaultIconKey { get; set; } = "folder";
        public bool DefaultAllowCustomerTicketCreation { get; set; } = true;
        public bool DefaultRequireApprovalForClose { get; set; } = false;
        public string Timeline { get; set; } = "default";
    }
}
