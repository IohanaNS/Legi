using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;

namespace Legi.Library.Application.Tests.Factories;

public static class LibraryDomainEventFactory
{
    public static BookAddedToLibraryDomainEvent BookAddedToLibrary(
        Guid? userBookId = null,
        Guid? userId = null,
        Guid? bookId = null,
        bool wishList = false,
        Guid? workId = null)
    {
        return new BookAddedToLibraryDomainEvent(
            userBookId ?? LibraryTestIds.UserBookId,
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            wishList);
    }

    public static ReadingStatusChangedDomainEvent ReadingStatusChanged(
        Guid? userId = null,
        Guid? bookId = null,
        ReadingStatus oldStatus = ReadingStatus.Reading,
        ReadingStatus newStatus = ReadingStatus.Finished,
        Guid? workId = null)
    {
        return new ReadingStatusChangedDomainEvent(
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            oldStatus,
            newStatus);
    }

    public static UserBookRatedDomainEvent UserBookRated(
        Guid? userId = null,
        Guid? bookId = null,
        Rating? oldRating = null,
        Rating? newRating = null,
        Guid? workId = null)
    {
        return new UserBookRatedDomainEvent(
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            oldRating,
            newRating ?? Rating.Create(8));
    }

    public static UserBookRatingRemovedDomainEvent UserBookRatingRemoved(
        Guid? userId = null,
        Guid? bookId = null,
        Rating? oldRating = null,
        Guid? workId = null)
    {
        return new UserBookRatingRemovedDomainEvent(
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            oldRating ?? Rating.Create(6));
    }

    public static ReadingProgressCreatedDomainEvent ReadingProgressCreated(
        Guid? readingPostId = null,
        Guid? userBookId = null,
        Guid? userId = null,
        Guid? bookId = null,
        string? content = "Halfway through, still engaged.",
        int? progressValue = 50,
        string? progressType = "Percentage",
        bool isSpoiler = false,
        Guid? workId = null)
    {
        return new ReadingProgressCreatedDomainEvent(
            readingPostId ?? LibraryTestIds.ReadingPostId,
            userBookId ?? LibraryTestIds.UserBookId,
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            content,
            progressValue,
            progressType,
            isSpoiler);
    }

    public static ReadingPostDeletedDomainEvent ReadingPostDeleted(
        Guid? readingPostId = null,
        Guid? userId = null,
        Guid? bookId = null,
        Guid? workId = null)
    {
        return new ReadingPostDeletedDomainEvent(
            readingPostId ?? LibraryTestIds.ReadingPostId,
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId);
    }
}
