using Legi.Library.Domain.Entities;
using Legi.Library.Domain.ValueObjects;

namespace Legi.Library.Application.Tests.Factories;

public sealed class ReadingProgressBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;
    private Guid _bookId = LibraryTestIds.BookId;
    private string? _content = "Halfway through, still engaged.";
    private Progress? _progress = Progress.CreatePercentage(50);
    private DateOnly? _readingDate = new(2026, 1, 15);
    private bool _isSpoiler;
    private bool _clearDomainEvents = true;

    public static ReadingProgressBuilder Valid() => new();

    public ReadingProgressBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public ReadingProgressBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ReadingProgressBuilder WithBookId(Guid bookId)
    {
        _bookId = bookId;
        return this;
    }

    public ReadingProgressBuilder WithContent(string? content)
    {
        _content = content;
        return this;
    }

    public ReadingProgressBuilder WithProgress(Progress? progress)
    {
        _progress = progress;
        return this;
    }

    public ReadingProgressBuilder WithReadingDate(DateOnly? readingDate)
    {
        _readingDate = readingDate;
        return this;
    }

    public ReadingProgressBuilder WithIsSpoiler(bool isSpoiler)
    {
        _isSpoiler = isSpoiler;
        return this;
    }

    public ReadingProgressBuilder KeepingDomainEvents()
    {
        _clearDomainEvents = false;
        return this;
    }

    public ReadingProgress Build()
    {
        var post = ReadingProgress.Create(
            _userBookId,
            _userId,
            _bookId,
            _content,
            _progress,
            _readingDate,
            _isSpoiler);

        if (_clearDomainEvents)
            post.ClearDomainEvents();

        return post;
    }
}
