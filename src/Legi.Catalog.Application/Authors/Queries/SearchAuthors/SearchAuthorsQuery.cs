using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Authors.Queries.SearchAuthors;

public record SearchAuthorsQuery(
    string SearchTerm,
    int Limit = 10
) : IRequest<SearchAuthorsResponse>;
