using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class ProjectMapper
{
    public static ProjectDto ToDto(this Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        Status = project.Status.ToString(),
        IconKey = project.IconKey,
        ProjectCategoryId = project.ProjectCategoryId,
        ProjectCategoryName = project.ProjectCategory?.Name,
        OwnerUserId = project.OwnerUserId,
        OwnerName = project.Members?.FirstOrDefault(m => m.UserId == project.OwnerUserId)?.User?.GetDisplayName() ?? string.Empty,
        MemberCount = project.Members?.Count ?? 0,

        AllowCustomerTicketCreation = project.AllowCustomerTicketCreation,
        RequireApprovalForClose = project.RequireApprovalForClose,

        NotificationEmail = project.NotificationEmail,
        CreatedAt = project.CreatedAt
    };

    public static IEnumerable<ProjectDto> ToDtoList(this IEnumerable<Project> projects)
        => projects.Select(p => p.ToDto());
}
