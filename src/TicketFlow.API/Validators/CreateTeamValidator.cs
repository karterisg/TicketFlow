using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class CreateTeamValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.OwnerUserId)
            .GreaterThan(0).WithMessage("Valid OwnerUserId is required.");
    }
}
