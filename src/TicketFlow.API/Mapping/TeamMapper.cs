using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class TeamMapper
{
    public static TeamDto ToDto(this Team team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        OwnerUserId = team.OwnerUserId,
        OwnerName = team.Members?.FirstOrDefault(m => m.UserId == team.OwnerUserId)?.User?.GetDisplayName() ?? string.Empty,
        MemberCount = team.Members?.Count ?? 0,
        ProjectCount = team.ProjectTeams?.Count ?? 0,
        CreatedAt = team.CreatedAt
    };

    public static IEnumerable<TeamDto> ToDtoList(this IEnumerable<Team> teams)
        => teams.Select(t => t.ToDto());
}
