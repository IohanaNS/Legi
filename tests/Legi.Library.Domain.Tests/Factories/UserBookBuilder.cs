using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.ValueObjects;

namespace Legi.Library.Domain.Tests.Factories;

public sealed class UserBookBuilder
{
    private Guid _userId = LibraryTestIds.UserId;
    private Guid _bookId = LibraryTestIds.BookId;
    private bool _wishList;
    private ReadingStatus? _status;
    private Progress? _progress;
    private Rating? _rating;
    private bool _clearDomainEvents = true;

    public static UserBookBuilder Valid() => new();

    public UserBookBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public UserBookBuilder WithBookId(Guid bookId)
    {
        _bookId = bookId;
        return this;
    }

    public UserBookBuilder AsWishlist()
    {
        _wishList = true;
        return this;
    }

    public UserBookBuilder WithStatus(ReadingStatus status)
    {
        _status = status;
        return this;
    }

    public UserBookBuilder WithProgress(Progress progress)
    {
        _progress = progress;
        return this;
    }

    public UserBookBuilder WithRating(Rating rating)
    {
        _rating = rating;
        return this;
    }

    public UserBookBuilder KeepingDomainEvents()
    {
        _clearDomainEvents = false;
        return this;
    }

    public UserBook Build()
    {
        var userBook = UserBook.Create(_userId, _bookId, _wishList);

        if (_status.HasValue)
            userBook.ChangeReadingStatus(_status.Value);

        if (_progress is not null)
            userBook.UpdateProgress(_progress);

        if (_rating is not null)
            userBook.Rate(_rating);

        if (_clearDomainEvents)
            userBook.ClearDomainEvents();

        return userBook;
    }
}
