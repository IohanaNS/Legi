using FluentValidation;

namespace Legi.Catalog.Application.Authors.Queries.GetPopularAuthors;

public class GetPopularAuthorsQueryValidator : AbstractValidator<GetPopularAuthorsQuery>
{
    public GetPopularAuthorsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50");
    }
}
