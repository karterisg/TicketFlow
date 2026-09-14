using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Services;

public class MeetingService(
    IMeetingRepository meetingRepository,
    AppDbContext context,
    ICacheService cache,
    ILogger<MeetingService> logger,
    INotificationService notificationService) : IMeetingService
{
    private const string AllMeetingsCacheKey = "meetings:all";

    // ─── Cache key helpers ───────────────────────────────────────────────────
    private static string MeetingKey(int id)
        => $"meeting:{id}";

    private static string AllMeetingsKey(int? projectId, int? teamId)
        => $"meetings:all:project={projectId}:team={teamId}";

    public async Task<IEnumerable<MeetingDto>> GetAllMeetingsAsync(int? projectId, int? teamId)
    {
        logger.LogInformation("Fetching all meetings for projectId={ProjectId} teamId={TeamId}", projectId, teamId);

        var cacheKey = AllMeetingsKey(projectId, teamId);
        var cached = await cache.GetAsync<IEnumerable<MeetingDto>>(cacheKey);
        if (cached != null)
        {
            logger.LogInformation("Returning cached all-meetings for project={ProjectId} team={TeamId}", projectId, teamId);
            return cached;
        }

        var meetings = await meetingRepository.GetAllAsync(projectId, teamId);
        var dto = meetings.ToDtoList();

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<PagedResultDto<MeetingDto>> GetPagedMeetingsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null)
    {
        logger.LogInformation(
            "Fetching meetings page {Page} (size {PageSize}, search='{Search}') for user={UserId} agent={AgentId}",
            page, pageSize, search, forUserId, forAgentId);

        var (items, totalCount) = await meetingRepository.GetPagedAsync(page, pageSize, search, forUserId, forAgentId);

        logger.LogInformation("Returned {Count}/{Total} meetings for page {Page}", items.Count, totalCount, page);

        return new PagedResultDto<MeetingDto>
        {
            Items = items.ToDtoList().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MeetingDto?> GetMeetingByIdAsync(int id)
    {
        logger.LogInformation("Fetching meeting by ID {MeetingId}", id);
        var cached = await cache.GetAsync<MeetingDto>(MeetingKey(id));

        if (cached != null)
        {
            logger.LogInformation("Returning cached meeting {MeetingId}", id);
            return cached;
        }

        var meeting = await meetingRepository.GetByIdAsync(id);
        if (meeting == null)
            return null;

        var dto = meeting.ToDto();
        await cache.SetAsync(MeetingKey(id), dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<Meeting> CreateMeetingAsync(CreateMeetingDto dto)
    {
        logger.LogInformation("Scheduling meeting {Title} at {ScheduledAt}", dto.Title, dto.ScheduledAt);

        if (dto.ScheduledAt <= DateTime.UtcNow)
            throw new ConflictException("Meetings must be scheduled in the future.");

        if (dto.ProjectId is null && dto.TeamId is null)
            throw new ConflictException("A meeting must be linked to a project or a team.");

        if (dto.ProjectId.HasValue)
        {
            var projectExists = await context.Projects.AnyAsync(p => p.Id == dto.ProjectId.Value);
            if (!projectExists)
                throw new NotFoundException("Project", dto.ProjectId.Value);
        }

        if (dto.TeamId.HasValue)
        {
            var teamExists = await context.Set<Team>().AnyAsync(t => t.Id == dto.TeamId.Value);
            if (!teamExists)
                throw new NotFoundException("Team", dto.TeamId.Value);
        }

        var meeting = new Meeting(dto.Title, dto.Description, dto.CreatedByUserId, dto.ScheduledAt, dto.DurationMinutes, dto.ProjectId, dto.TeamId, dto.LocationOrLink);

        var attendeeUserIds = new HashSet<int>();
        var attendeeAgentIds = new HashSet<int>();

        if (dto.ProjectId.HasValue)
        {
            var projectMembers = await context.Set<ProjectMember>()
                .Where(m => m.ProjectId == dto.ProjectId.Value)
                .ToListAsync();

            foreach (var member in projectMembers)
            {
                if (member.UserId.HasValue && attendeeUserIds.Add(member.UserId.Value))
                    meeting.Attendees.Add(new MeetingAttendee { UserId = member.UserId });
                else if (member.AgentId.HasValue && attendeeAgentIds.Add(member.AgentId.Value))
                    meeting.Attendees.Add(new MeetingAttendee { AgentId = member.AgentId });
            }
        }

        if (dto.TeamId.HasValue)
        {
            var teamMembers = await context.Set<TeamMember>()
                .Where(m => m.TeamId == dto.TeamId.Value)
                .ToListAsync();

            foreach (var member in teamMembers)
            {
                if (member.UserId.HasValue && attendeeUserIds.Add(member.UserId.Value))
                    meeting.Attendees.Add(new MeetingAttendee { UserId = member.UserId });
                else if (member.AgentId.HasValue && attendeeAgentIds.Add(member.AgentId.Value))
                    meeting.Attendees.Add(new MeetingAttendee { AgentId = member.AgentId });
            }
        }

        foreach (var attendee in dto.Attendees)
        {
            var isDuplicateUser = attendee.UserId.HasValue && attendeeUserIds.Contains(attendee.UserId.Value);
            var isDuplicateAgent = attendee.AgentId.HasValue && attendeeAgentIds.Contains(attendee.AgentId.Value);
            if (isDuplicateUser || isDuplicateAgent)
                continue;

            if (attendee.UserId.HasValue)
                attendeeUserIds.Add(attendee.UserId.Value);
            if (attendee.AgentId.HasValue)
                attendeeAgentIds.Add(attendee.AgentId.Value);

            meeting.Attendees.Add(new MeetingAttendee { UserId = attendee.UserId, AgentId = attendee.AgentId });
        }

        var created = await meetingRepository.CreateAsync(meeting);

        logger.LogInformation("Meeting {MeetingId} scheduled successfully", created.Id);

        await cache.RemoveAsync(AllMeetingsKey(dto.ProjectId, dto.TeamId));
        await cache.RemoveAsync(AllMeetingsKey(null, null));

        foreach (var attendee in created.Attendees.Where(a => a.UserId.HasValue))
        {
            await notificationService.NotifyAsync(attendee.UserId!.Value,
                $"You've been invited to '{created.Title}' on {created.ScheduledAt:g}.", "info");
        }

        return created;
    }

    public async Task CancelMeetingAsync(int id, int? actingUserId, bool isGlobalManager)
    {
        var meeting = await meetingRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Meeting", id);

        if (!isGlobalManager && (actingUserId is null || meeting.CreatedByUserId != actingUserId))
            throw new ForbiddenException("Only the organizer or a manager can cancel this meeting.");

        meeting.Cancel();
        await meetingRepository.UpdateAsync(meeting);

        await cache.RemoveAsync(MeetingKey(id));
        await cache.RemoveAsync(AllMeetingsKey(meeting.ProjectId, meeting.TeamId));
        await cache.RemoveAsync(AllMeetingsKey(null, null));

        foreach (var attendee in meeting.Attendees.Where(a => a.UserId.HasValue))
        {
            await notificationService.NotifyAsync(attendee.UserId!.Value,
                $"Meeting '{meeting.Title}' has been cancelled.", "warning");
        }
    }

    public async Task RespondAsync(int id, RespondToMeetingDto dto)
    {
        if (!Enum.TryParse<AttendeeResponse>(dto.Response, true, out var response))
            throw new ConflictException("Invalid response value.");

        var attendee = await context.Set<MeetingAttendee>()
            .FirstOrDefaultAsync(a => a.MeetingId == id
                && ((dto.UserId.HasValue && a.UserId == dto.UserId) || (dto.AgentId.HasValue && a.AgentId == dto.AgentId)))
            ?? throw new NotFoundException("MeetingAttendee", id);

        attendee.Response = response;
        await context.SaveChangesAsync();

        var meeting = await context.Set<Meeting>().FirstAsync(m => m.Id == id);

        await cache.RemoveAsync(MeetingKey(id));
        await cache.RemoveAsync(AllMeetingsKey(meeting.ProjectId, meeting.TeamId));
        await cache.RemoveAsync(AllMeetingsKey(null, null));
    }
}



