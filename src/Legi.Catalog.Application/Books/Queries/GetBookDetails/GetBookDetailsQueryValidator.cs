using FluentValidation;

namespace Legi.Catalog.Application.Books.Queries.GetBookDetails;

public class GetBookDetailsQueryValidator : AbstractValidator<GetBookDetailsQuery>
{
    public GetBookDetailsQueryValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("Book ID is required");
    }
}