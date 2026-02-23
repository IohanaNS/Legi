using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Tags.Queries.SearchTags;

public record SearchTagsQuery(
    string SearchTerm,
    int Limit = 10
) : IRequest<SearchTagsResponse>;
