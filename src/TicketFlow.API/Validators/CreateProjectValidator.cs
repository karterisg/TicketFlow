using FluentValidation;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    private static readonly string[] AllowedIcons =
        { "folder", "rocket", "bug", "star", "briefcase", "gear", "flag", "shield" };

    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.OwnerUserId)
            .GreaterThan(0).WithMessage("Valid OwnerUserId is required.");

        RuleFor(x => x.ProjectCategoryId)
            .GreaterThan(0).WithMessage("Valid ProjectCategoryId is required.")
            .When(x => x.ProjectCategoryId.HasValue);

        RuleFor(x => x.IconKey)
            .Must(icon => AllowedIcons.Contains(icon))
            .WithMessage($"IconKey must be one of: {string.Join(", ", AllowedIcons)}.");
    }
}
