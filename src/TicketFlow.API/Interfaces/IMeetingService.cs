using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Interfaces;

public interface IMeetingService
{
    Task<IEnumerable<MeetingDto>> GetAllMeetingsAsync(int? projectId, int? teamId);
    Task<PagedResultDto<MeetingDto>> GetPagedMeetingsAsync(int page, int pageSize, string? search, int? forUserId = null, int? forAgentId = null);
    Task<MeetingDto?> GetMeetingByIdAsync(int id);
    Task<Meeting> CreateMeetingAsync(CreateMeetingDto dto);
    Task CancelMeetingAsync(int id, int? actingUserId, bool isGlobalManager);
    Task RespondAsync(int id, RespondToMeetingDto dto);
}
