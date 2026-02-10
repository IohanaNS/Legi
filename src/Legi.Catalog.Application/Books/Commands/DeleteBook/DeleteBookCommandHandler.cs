using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.DeleteBook;

public class DeleteBookCommandHandler(IBookRepository bookRepository)
    : IRequestHandler<DeleteBookCommand>
{
    public async Task Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        // 1. Get book
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);

        if (book == null)
            throw new NotFoundException("Book", request.BookId);

        // 2. Delete book (cascade will handle authors and tags junction tables)
        await bookRepository.DeleteAsync(book, cancellationToken);
    }
}