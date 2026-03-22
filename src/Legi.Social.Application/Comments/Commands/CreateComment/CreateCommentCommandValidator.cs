using FluentValidation;
using Legi.Social.Domain.Entities;

namespace Legi.Social.Application.Comments.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Comment content cannot be empty.")
            .MaximumLength(Comment.MaxContentLength)
            .WithMessage($"Comment content cannot exceed {Comment.MaxContentLength} characters.");
    }
}