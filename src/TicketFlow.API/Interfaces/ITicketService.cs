using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Interfaces;

public interface ITicketService
{
    Task<IEnumerable<TicketDto>> GetAllTicketsAsync(int? forUserId = null, int? forAgentId = null);
    Task<PagedResultDto<TicketDto>> GetPagedTicketsAsync(
        int page, int pageSize, string? search, List<string>? statuses, int? forUserId = null, int? forAgentId = null);
    Task<TicketStatsDto> GetTicketStatsAsync(int? forAgentId = null);
    Task<TicketDto?> GetTicketByIdAsync(int id);
    Task<Ticket> CreateTicketAsync(string title, string description, int userId, int categoryId, string priority = "Medium", DateTime? dateTime = null, int? projectId = null);
    Task AssignTicketAsync(int ticketId, int agentId);
    Task SetDueDateAsync(int ticketId, DateTime? dueDate);
    Task ResolveTicketAsync(int ticketId);
    Task CloseTicketAsync(int ticketId, bool isManager);
    Task DeleteTicketAsync(int ticketId);
}