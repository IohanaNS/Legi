using FluentValidation;

namespace Legi.Library.Application.UserBooks.Commands.RateUserBook;

public class RateUserBookCommandValidator : AbstractValidator<RateUserBookCommand>
{
    public RateUserBookCommandValidator()
    {
        RuleFor(x => x.UserBookId)
            .NotEmpty().WithMessage("UserBook ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Stars)
            .InclusiveBetween(0.5m, 5.0m)
            .WithMessage("Rating must be between 0.5 and 5.0 stars.");

        RuleFor(x => x.Stars)
            .Must(s => s % 0.5m == 0)
            .WithMessage("Rating must be in increments of 0.5 stars.");
    }
}