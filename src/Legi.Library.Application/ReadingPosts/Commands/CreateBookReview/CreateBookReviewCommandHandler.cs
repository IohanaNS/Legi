using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.CreateBookReview;

/// <summary>
/// Writes a book review: sets the user's rating on the <c>UserBook</c> (flowing to
/// Catalog's average via <c>UserBookRated</c>, flagged <c>IsPartOfReview</c> so
/// Social suppresses the standalone BookRated feed item) and creates a rated,
/// content-only <c>ReadingProgress</c> that fans out as a <c>ReviewCreated</c>
/// activity. Both are persisted in a single transaction.
/// </summary>
public class CreateBookReviewCommandHandler
    : IRequestHandler<CreateBookReviewCommand, CreateBookReviewResponse>
{
    private readonly IReadingPostRepository _readingPostRepository;
    private readonly IUserBookRepository _userBookRepository;

    public CreateBookReviewCommandHandler(
        IReadingPostRepository readingPostRepository,
        IUserBookRepository userBookRepository)
    {
        _readingPostRepository = readingPostRepository;
        _userBookRepository = userBookRepository;
    }

    public async Task<CreateBookReviewResponse> Handle(
        CreateBookReviewCommand request,
        CancellationToken cancellationToken)
    {
        var userBook = await _userBookRepository.GetByIdAsync(
                           request.UserBookId, cancellationToken)
                       ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        var rating = Rating.FromStars(request.Stars);

        // Set the rating as part of the review so Catalog's average updates while
        // Social emits a single ReviewCreated activity (no duplicate BookRated).
        userBook.Rate(rating, isPartOfReview: true);

        var review = ReadingProgress.CreateReview(
            userBook.Id,
            userBook.UserId,
            userBook.BookId,
            userBook.WorkId,
            request.Content,
            rating,
            request.IsSpoiler);

        await _readingPostRepository.AddAsync(review, cancellationToken);
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        return new CreateBookReviewResponse(
            review.Id,
            review.UserBookId,
            review.Content!,
            review.IsSpoiler,
            rating.Stars,
            review.CreatedAt);
    }
}
