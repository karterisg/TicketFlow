using TicketFlow.API.Exceptions;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Services;

public class RatingService(
    ITicketRepository ticketRepository,
    IRatingRepository ratingRepository,
    ITicketActivityRepository activityRepository) : IRatingService
{
    public async Task<Rating> SubmitRatingAsync(int ticketId, int userId, int score, string? comment)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (ticket.UserId != userId)
            throw new ForbiddenException("Only the ticket's customer can rate it.");

        if (ticket.Status is not (TicketStatus.Resolved or TicketStatus.Closed))
            throw new BadRequestException("Ticket must be resolved or closed before it can be rated.");

        var existing = await ratingRepository.GetByTicketIdAsync(ticketId);
        if (existing is not null)
            throw new ConflictException("This ticket has already been rated.");

        var rating = new Rating(ticketId, userId, score, comment);
        await ratingRepository.AddAsync(rating);

        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.RatingSubmitted,
            $"Customer rated the ticket {score}/5.", userId, ticket.User.GetDisplayName()));

        return rating;
    }

    public Task<Rating?> GetRatingAsync(int ticketId) => ratingRepository.GetByTicketIdAsync(ticketId);
}
