using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Interfaces;

public interface IRatingService
{
    Task<Rating> SubmitRatingAsync(int ticketId, int userId, int score, string? comment);
    Task<Rating?> GetRatingAsync(int ticketId);
}
