using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class CreateAgentValidator : AbstractValidator<CreateAgentDto>
{
    public CreateAgentValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");


        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.")
            .Must(email => email.EndsWith(".com", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Email must end with '.com'.");
    }
}