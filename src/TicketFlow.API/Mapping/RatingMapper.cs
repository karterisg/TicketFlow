using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Mapping;

public static class RatingMapper
{
    public static RatingDto ToDto(this Rating rating) => new()
    {
        Id = rating.Id,
        TicketId = rating.TicketId,
        UserId = rating.UserId,
        Score = rating.Score,
        Comment = rating.Comment
    };
}
