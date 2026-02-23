using Legi.Catalog.Application.Tags.Queries.SearchTags;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Tags.Queries.GetPopularTags;

public class GetPopularTagsQueryHandler(ITagReadRepository tagReadRepository)
    : IRequestHandler<GetPopularTagsQuery, SearchTagsResponse>
{
    public async Task<SearchTagsResponse> Handle(
        GetPopularTagsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await tagReadRepository.GetPopularAsync(
            request.Limit,
            cancellationToken);

        var tags = results
            .Select(t => new TagResult(t.Name, t.Slug, t.UsageCount))
            .ToList();

        return new SearchTagsResponse(tags);
    }
}
