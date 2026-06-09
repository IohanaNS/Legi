using FluentValidation;
using Legi.Library.Domain.Entities;

namespace Legi.Library.Application.ReadingPosts.Commands.CreateBookReview;

public class CreateBookReviewCommandValidator : AbstractValidator<CreateBookReviewCommand>
{
    public CreateBookReviewCommandValidator()
    {
        RuleFor(x => x.UserBookId)
            .NotEmpty().WithMessage("UserBook ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Review content is required.")
            .MinimumLength(ReadingProgress.MinReviewContentLength)
            .WithMessage($"Review must have at least {ReadingProgress.MinReviewContentLength} characters.")
            .MaximumLength(ReadingProgress.MaxContentLength)
            .WithMessage($"Review must have at most {ReadingProgress.MaxContentLength} characters.");

        RuleFor(x => x.Stars)
            .InclusiveBetween(0.5m, 5.0m)
            .WithMessage("Rating must be between 0.5 and 5.0 stars.");

        RuleFor(x => x.Stars)
            .Must(s => s % 0.5m == 0)
            .WithMessage("Rating must be in increments of 0.5 stars.");
    }
}
