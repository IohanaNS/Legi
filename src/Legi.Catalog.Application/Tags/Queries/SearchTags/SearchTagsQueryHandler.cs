using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Tags.Queries.SearchTags;

public class SearchTagsQueryHandler(ITagReadRepository tagReadRepository)
    : IRequestHandler<SearchTagsQuery, SearchTagsResponse>
{
    public async Task<SearchTagsResponse> Handle(
        SearchTagsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await tagReadRepository.SearchAsync(
            request.SearchTerm,
            request.Limit,
            cancellationToken);

        var tags = results
            .Select(t => new TagResult(t.Name, t.Slug, t.UsageCount))
            .ToList();

        return new SearchTagsResponse(tags);
    }
}
