using FluentValidation;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;

public class UpdateReadingPostCommandValidator : AbstractValidator<UpdateReadingPostCommand>
{
    public UpdateReadingPostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("Post ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content)
                       || x.ProgressValue.HasValue)
            .WithMessage("Post must have content or progress (or both).");

        RuleFor(x => x.Content)
            .MaximumLength(ReadingPost.MaxContentLength)
            .When(x => x.Content is not null)
            .WithMessage($"Content must be at most {ReadingPost.MaxContentLength} characters.");

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