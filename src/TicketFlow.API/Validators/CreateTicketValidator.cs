using FluentValidation;
using TicketFlow.Shared.DTOs;


namespace TicketFlow.API.Validators;

public class CreateTicketValidator : AbstractValidator<CreateTicketDto>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid UserId is required.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Valid CategoryId is required.");

            RuleFor(x => x.Priority)
                .Must(p => new[] { "Low", "Medium", "High", "Critical" }.Contains(p))
                .WithMessage("Priority must be Low, Medium, High, or Critical.");
        
    }
}
