using Legi.Catalog.Application.Authors.Queries.SearchAuthors;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Authors.Queries.GetPopularAuthors;

public record GetPopularAuthorsQuery(int Limit = 20) : IRequest<SearchAuthorsResponse>;
