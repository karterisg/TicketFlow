using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class AgentMapper
{
    public static AgentDto ToDto(this Agent agent) => new()
    {
        Id = agent.Id,
        FullName = agent.FullName,
        Email = agent.Email,
        IsAvailable = agent.IsAvailable,
        AssignedTicketCount = agent.AssignedTickets?.Count ?? 0
    };
    public static IEnumerable<AgentDto> ToDtoList(this IEnumerable<Agent> agents)
        => agents.Select(a => a.ToDto());
}