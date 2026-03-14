using FluentValidation;

namespace Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

public class AddBookToLibraryCommandValidator : AbstractValidator<AddBookToLibraryCommand>
{
    public AddBookToLibraryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required.");
    }
}