using Legi.Catalog.Application.Tags.Queries.SearchTags;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Tags.Queries.GetPopularTags;

public record GetPopularTagsQuery(int Limit = 20) : IRequest<SearchTagsResponse>;
