using FluentValidation;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksQueryValidator : AbstractValidator<SearchBooksQuery>
{
    public SearchBooksQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        When(x => x.MinRating.HasValue, () =>
        {
            RuleFor(x => x.MinRating!.Value)
                .InclusiveBetween(0, 5)
                .WithMessage("Minimum rating must be between 0 and 5");
        });
    }
}