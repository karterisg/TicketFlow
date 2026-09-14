using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class MeetingMapper
{
    public static MeetingDto ToDto(this Meeting meeting) => new()
    {
        Id = meeting.Id,
        Title = meeting.Title,
        Description = meeting.Description,
        ProjectId = meeting.ProjectId,
        ProjectName = meeting.Project?.Name,
        TeamId = meeting.TeamId,
        TeamName = meeting.Team?.Name,
        CreatedByUserId = meeting.CreatedByUserId,
        ScheduledAt = meeting.ScheduledAt,
        DurationMinutes = meeting.DurationMinutes,
        LocationOrLink = meeting.LocationOrLink,
        Status = meeting.Status.ToString(),
        AttendeeCount = meeting.Attendees?.Count ?? 0,
        CreatedAt = meeting.CreatedAt,
        Attendees = meeting.Attendees?.Select(a => a.ToDto()).ToList() ?? new()
    };

    public static IEnumerable<MeetingDto> ToDtoList(this IEnumerable<Meeting> meetings)
        => meetings.Select(m => m.ToDto());

    public static MeetingAttendeeDto ToDto(this MeetingAttendee attendee) => new()
    {
        Id = attendee.Id,
        MeetingId = attendee.MeetingId,
        UserId = attendee.UserId,
        AgentId = attendee.AgentId,
        AttendeeName = attendee.User?.GetDisplayName() ?? attendee.Agent?.GetDisplayName() ?? string.Empty,
        Response = attendee.Response.ToString()
    };
}
