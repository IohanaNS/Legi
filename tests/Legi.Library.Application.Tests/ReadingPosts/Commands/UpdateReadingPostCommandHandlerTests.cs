using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.Commands;

public class UpdateReadingPostCommandHandlerTests
{
    private readonly Mock<IReadingPostRepository> _readingPostRepository = new();
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly UpdateReadingPostCommandHandler _handler;

    public UpdateReadingPostCommandHandlerTests()
    {
        _handler = new UpdateReadingPostCommandHandler(
            _readingPostRepository.Object,
            _userBookRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_PercentageProgressProvided_UpdatesPost()
    {
        var post = ReadingProgressBuilder.Valid().Build();
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithPostId(post.Id)
            .WithContent("Updated note.")
            .WithProgress(75, ProgressType.Percentage)
            .WithIsSpoiler(true)
            .Build();

        _readingPostRepository
            .Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        _readingPostRepository
            .Setup(r => r.UpdateAsync(post, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Updated note.", post.Content);
        Assert.True(post.IsSpoiler);
        Assert.Equal(75, post.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, post.CurrentProgress?.Type);
        Assert.Equal(75, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
        Assert.True(response.IsSpoiler);
        _readingPostRepository.Verify(r => r.UpdateAsync(post, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageProgressEqualsPageCount_CompletesPostAndUserBook()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        var post = ReadingProgressBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithBookId(userBook.BookId)
            .Build();
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithPostId(post.Id)
            .WithProgress(464, ProgressType.Page)
            .Build();

        _readingPostRepository
            .Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        _userBookRepository
            .Setup(r => r.GetByIdAsync(post.UserBookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(post.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: post.BookId, pageCount: 464));
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _readingPostRepository
            .Setup(r => r.UpdateAsync(post, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(100, post.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, post.CurrentProgress?.Type);
        Assert.Equal(100, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageProgressExceedsPageCount_ThrowsDomainException()
    {
        var post = ReadingProgressBuilder.Valid().Build();
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithPostId(post.Id)
            .WithProgress(465, ProgressType.Page)
            .Build();

        _readingPostRepository
            .Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(post.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: post.BookId, pageCount: 464));

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _readingPostRepository.Verify(
            r => r.UpdateAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnPost_ThrowsForbiddenException()
    {
        var post = ReadingProgressBuilder.Valid().Build();
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithPostId(post.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();

        _readingPostRepository
            .Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _readingPostRepository.Verify(
            r => r.UpdateAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
