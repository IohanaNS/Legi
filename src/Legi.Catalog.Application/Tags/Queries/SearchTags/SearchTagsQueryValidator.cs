using FluentValidation;

namespace Legi.Catalog.Application.Tags.Queries.SearchTags;

public class SearchTagsQueryValidator : AbstractValidator<SearchTagsQuery>
{
    public SearchTagsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("Search term is required")
            .MaximumLength(100)
            .WithMessage("Search term must be at most 100 characters");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50");
    }
}
