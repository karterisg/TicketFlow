using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class CreateMeetingValidator : AbstractValidator<CreateMeetingDto>
{
    public CreateMeetingValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

        RuleFor(x => x.CreatedByUserId)
            .GreaterThan(0).WithMessage("Valid CreatedByUserId is required.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Meetings must be scheduled in the future.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(5, 480).WithMessage("Duration must be between 5 and 480 minutes.");

        RuleFor(x => x)
            .Must(x => x.ProjectId.HasValue || x.TeamId.HasValue)
            .WithMessage("A meeting must be linked to a project or a team.");
    }
}
