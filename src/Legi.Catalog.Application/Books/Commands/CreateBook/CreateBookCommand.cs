using Legi.Catalog.Application.Common.Mediator;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public record CreateBookCommand(
    string Isbn,
    string Title,
    List<string> Authors,
    Guid CreatedByUserId,
    string? Synopsis = null,
    int? PageCount = null,
    string? Publisher = null,
    string? CoverUrl = null,
    List<string>? Tags = null
) : IRequest<CreateBookResponse>;