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
            .MinimumLength(UserList.MinNameLength)
            .WithMessage($"List name must be at least {UserList.MinNameLength} characters.")
            .MaximumLength(UserList.MaxNameLength)
            .WithMessage($"List name must be at most {UserList.MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(UserList.MaxDescriptionLength)
            .When(x => x.Description is not null)
            .WithMessage($"Description must be at most {UserList.MaxDescriptionLength} characters.");
    }
}