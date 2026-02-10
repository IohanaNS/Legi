using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.DeleteBook;

public record DeleteBookCommand(Guid BookId) : IRequest;