using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class SubmitRatingValidator : AbstractValidator<SubmitRatingDto>
{
    public SubmitRatingValidator()
    {
        RuleFor(x => x.Score)
            .InclusiveBetween(1, 5).WithMessage("Score must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}
