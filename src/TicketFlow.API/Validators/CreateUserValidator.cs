using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.")
            .Must(email => email.EndsWith(".com", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Email must end with '.com'.");
    }
}