using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.Commands.SetBookCover;

public sealed class SetBookCoverCommandHandler(
    IBookRepository bookRepository,
    IWorkRepository workRepository,
    ILogger<SetBookCoverCommandHandler> logger)
    : IRequestHandler<SetBookCoverCommand, SetBookCoverResponse>
{
    public async Task<SetBookCoverResponse> Handle(
        SetBookCoverCommand request,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null)
            throw new NotFoundException(nameof(Book), request.BookId);

        // Fill-only: anyone may add a cover to a cover-less book, but a present
        // cover can't be overwritten (no vandalising good covers via this path).
        // The controller cleans up the just-stored blob if this guard rejects.
        if (!string.IsNullOrWhiteSpace(book.CoverUrl))
            throw new ConflictException("This book already has a cover.");

        book.UpdateDetails(coverUrl: request.CoverUrl);
        book.RaiseUpdatedEvent(); // republish so Library/Social snapshots get the cover
        await bookRepository.UpdateAsync(book, cancellationToken);

        // Backfill the work's default cover if it still lacks one.
        var work = await workRepository.GetByIdAsync(book.WorkId, cancellationToken);
        if (work is not null && string.IsNullOrWhiteSpace(work.DefaultCoverUrl))
        {
            work.EnsureDefaultCover(request.CoverUrl);
            await workRepository.UpdateAsync(work, cancellationToken);
        }

        logger.LogInformation(
            "User {UserId} set a manual cover for book {BookId}", request.UserId, request.BookId);

        return new SetBookCoverResponse(request.CoverUrl);
    }
}
