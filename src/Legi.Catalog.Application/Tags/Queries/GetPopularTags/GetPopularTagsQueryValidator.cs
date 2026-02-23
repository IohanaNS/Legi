using FluentValidation;

namespace Legi.Catalog.Application.Tags.Queries.GetPopularTags;

public class GetPopularTagsQueryValidator : AbstractValidator<GetPopularTagsQuery>
{
    public GetPopularTagsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50");
    }
}
