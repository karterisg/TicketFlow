namespace TicketFlow.Shared.Domain;

public class ProjectTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultProjectCategoryId { get; set; }
    public string DefaultIconKey { get; set; } = "folder";
    public bool DefaultAllowCustomerTicketCreation { get; set; } = true;
    public bool DefaultRequireApprovalForClose { get; set; } = false;

    public ProjectCategory? DefaultProjectCategory { get; set; }

    public string Timeline { get; set; } = "default";
}
