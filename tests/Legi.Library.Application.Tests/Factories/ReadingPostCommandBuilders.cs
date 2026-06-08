using Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;
using Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.Tests.Factories;

public sealed class CreateReadingPostCommandBuilder
{
    private Guid _userBookId = LibraryTestIds.UserBookId;
    private Guid _userId = LibraryTestIds.UserId;
    private string? _content = "Halfway through, still engaged.";
    private int? _progressValue = 50;
    private ProgressType? _progressType = ProgressType.Percentage;
    private DateOnly? _readingDate = new(2026, 1, 15);
    private bool _isSpoiler;

    public static CreateReadingPostCommandBuilder Valid() => new();

    public CreateReadingPostCommandBuilder WithUserBookId(Guid userBookId)
    {
        _userBookId = userBookId;
        return this;
    }

    public CreateReadingPostCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public CreateReadingPostCommandBuilder WithContent(string? content)
    {
        _content = content;
        return this;
    }

    public CreateReadingPostCommandBuilder WithProgress(int? value, ProgressType? type)
    {
        _progressValue = value;
        _progressType = type;
        return this;
    }

    public CreateReadingPostCommandBuilder WithReadingDate(DateOnly? readingDate)
    {
        _readingDate = readingDate;
        return this;
    }

    public CreateReadingPostCommandBuilder WithIsSpoiler(bool isSpoiler)
    {
        _isSpoiler = isSpoiler;
        return this;
    }

    public CreateReadingPostCommand Build()
    {
        return new CreateReadingPostCommand(
            _userBookId,
            _userId,
            _content,
            _progressValue,
            _progressType,
            _readingDate,
            _isSpoiler);
    }
}

public sealed class UpdateReadingPostCommandBuilder
{
    private Guid _postId = LibraryTestIds.ReadingPostId;
    private Guid _userId = LibraryTestIds.UserId;
    private string? _content = "Updated note.";
    private int? _progressValue = 75;
    private ProgressType? _progressType = ProgressType.Percentage;
    private bool _isSpoiler;

    public static UpdateReadingPostCommandBuilder Valid() => new();

    public UpdateReadingPostCommandBuilder WithPostId(Guid postId)
    {
        _postId = postId;
        return this;
    }

    public UpdateReadingPostCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public UpdateReadingPostCommandBuilder WithContent(string? content)
    {
        _content = content;
        return this;
    }

    public UpdateReadingPostCommandBuilder WithProgress(int? value, ProgressType? type)
    {
        _progressValue = value;
        _progressType = type;
        return this;
    }

    public UpdateReadingPostCommandBuilder WithIsSpoiler(bool isSpoiler)
    {
        _isSpoiler = isSpoiler;
        return this;
    }

    public UpdateReadingPostCommand Build()
    {
        return new UpdateReadingPostCommand(
            _postId,
            _userId,
            _content,
            _progressValue,
            _progressType,
            _isSpoiler);
    }
}
