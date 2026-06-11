using FluentValidation;
using Legi.Library.Domain.Entities;

namespace Legi.Library.Application.UserLists.Commands.UpdateUserList;

public class UpdateUserListCommandValidator : AbstractValidator<UpdateUserListCommand>
{
    public UpdateUserListCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("List name cannot be blank.")
            .MinimumLength(UserList.MinNameLength)
            .WithMessage($"List name must be at least {UserList.MinNameLength} characters.")
            .MaximumLength(UserList.MaxNameLength)
            .WithMessage($"List name must be at most {UserList.MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(UserList.MaxDescriptionLength)
            .When(x => x.Description is not null)
            .WithMessage($"Description must be at most {UserList.MaxDescriptionLength} characters.");

        RuleFor(x => x.BookIds)
            .NotNull().WithMessage("BookIds is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("A list cannot contain the same book twice.");
    }
}