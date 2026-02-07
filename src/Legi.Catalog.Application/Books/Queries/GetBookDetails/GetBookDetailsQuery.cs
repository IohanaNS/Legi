using Legi.Catalog.Application.Common.Mediator;

namespace Legi.Catalog.Application.Books.Queries.GetBookDetails;

public record GetBookDetailsQuery(Guid BookId) : IRequest<GetBookDetailsResponse>;