using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.UpdateBook;

public record UpdateBookCommand(
    Guid BookId,
    string? Title = null,
    string? Synopsis = null,
    int? PageCount = null,
    string? Publisher = null,
    string? CoverUrl = null,
    List<string>? Authors = null,
    List<string>? Tags = null
) : IRequest<UpdateBookResponse>;