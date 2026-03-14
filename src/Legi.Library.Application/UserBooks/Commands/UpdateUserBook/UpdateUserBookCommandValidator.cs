using FluentValidation;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.UserBooks.Commands.UpdateUserBook;

public class UpdateUserBookCommandValidator : AbstractValidator<UpdateUserBookCommand>
{
    public UpdateUserBookCommandValidator()
    {
        RuleFor(x => x.UserBookId)
            .NotEmpty().WithMessage("UserBook ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("Invalid reading status.");

        // Progress: both value and type must be provided together, or neither
        RuleFor(x => x.ProgressType)
            .NotNull().When(x => x.ProgressValue.HasValue)
            .WithMessage("Progress type is required when progress value is provided.");

        RuleFor(x => x.ProgressValue)
            .NotNull().When(x => x.ProgressType.HasValue)
            .WithMessage("Progress value is required when progress type is provided.");

        RuleFor(x => x.ProgressValue)
            .GreaterThanOrEqualTo(0).When(x => x.ProgressValue.HasValue)
            .WithMessage("Progress value cannot be negative.");

        RuleFor(x => x.ProgressValue)
            .LessThanOrEqualTo(100)
            .When(x => x.ProgressValue.HasValue && x.ProgressType == ProgressType.Percentage)
            .WithMessage("Percentage progress cannot exceed 100.");
    }
}