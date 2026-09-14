using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class TicketMapper
{
    public static TicketDto ToDto(this Ticket ticket) => new()
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        Status = ticket.Status.ToString(),
        Priority = ticket.Priority.ToString(),
        CreatedAt = ticket.CreatedAt,
        ResolvedAt = ticket.ResolvedAt,
        UserId = ticket.UserId,
        UserName = ticket.User?.GetDisplayName() ?? string.Empty,
        AgentId = ticket.AgentId,
        AgentName = ticket.Agent?.GetDisplayName(),
        CategoryName = ticket.Category?.Name ?? string.Empty,
        CommentCount = ticket.Comments?.Count ?? 0,
        TicketNumber = ticket.TicketNumber,

        DueDate = ticket.DueDate,

        ProjectId = ticket.ProjectId,
        ProjectName = ticket.Project?.Name
    };



    public static IEnumerable<TicketDto> ToDtoList(this IEnumerable<Ticket> tickets)
        => tickets.Select(t => t.ToDto());
}