using Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;
using Legi.Library.Application.UserBooks.Commands.RateUserBook;
using Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;
using Legi.Library.Application.UserBooks.Commands.RemoveUserBookRating;
using Legi.Library.Application.UserBooks.Commands.UpdateUserBook;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.Tests.Factories;

public sealed class AddBookToLibraryCommandBuilder
{
    private Guid _userId = LibraryTestIds.UserId;
    private Guid _bookId = LibraryTestIds.BookId;
    private bool _wishlist;

    public static AddBookToLibraryCommandBuilder Valid() => new();

    public AddBookToLibraryCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public AddBookToLibraryCommandBuilder WithBookId(Guid bookId)
    {
        _bookId = bookId;
        return this;
    }

    public AddBookToLibraryCommandBuilder AsWishlist()
    {
        _wishlist = true;
        return this;
    }

    public AddBookToLibraryCommand Build() => new(_userId, _bookId, _wishlist);
}

public sealed class UpdateUserBookCommandBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;
    private ReadingStatus? _status;
    private bool? _wishlist;
    private int? _progressValue;
    private ProgressType? _progressType;
    private DateOnly? _finishedReadingAt;

    public static UpdateUserBookCommandBuilder Valid() => new();

    public UpdateUserBookCommandBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public UpdateUserBookCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public UpdateUserBookCommandBuilder WithStatus(ReadingStatus? status)
    {
        _status = status;
        return this;
    }

    public UpdateUserBookCommandBuilder WithWishlist(bool? wishlist)
    {
        _wishlist = wishlist;
        return this;
    }

    public UpdateUserBookCommandBuilder WithProgress(int? value, ProgressType? type)
    {
        _progressValue = value;
        _progressType = type;
        return this;
    }

    public UpdateUserBookCommandBuilder WithFinishedReadingAt(DateOnly? finishedReadingAt)
    {
        _finishedReadingAt = finishedReadingAt;
        return this;
    }

    public UpdateUserBookCommand Build()
    {
        return new UpdateUserBookCommand(
            _userBookId,
            _userId,
            _status,
            _wishlist,
            _progressValue,
            _progressType,
            _finishedReadingAt);
    }
}

public sealed class RateUserBookCommandBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;
    private decimal _stars = 4.0m;

    public static RateUserBookCommandBuilder Valid() => new();

    public RateUserBookCommandBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public RateUserBookCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public RateUserBookCommandBuilder WithStars(decimal stars)
    {
        _stars = stars;
        return this;
    }

    public RateUserBookCommand Build() => new(_userBookId, _userId, _stars);
}

public sealed class RemoveUserBookRatingCommandBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;

    public static RemoveUserBookRatingCommandBuilder Valid() => new();

    public RemoveUserBookRatingCommandBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public RemoveUserBookRatingCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public RemoveUserBookRatingCommand Build() => new(_userBookId, _userId);
}

public sealed class RemoveBookFromLibraryCommandBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;

    public static RemoveBookFromLibraryCommandBuilder Valid() => new();

    public RemoveBookFromLibraryCommandBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public RemoveBookFromLibraryCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public RemoveBookFromLibraryCommand Build() => new(_userBookId, _userId);
}
