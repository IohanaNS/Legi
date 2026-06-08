using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.Commands;

public class CreateReadingPostCommandHandlerTests
{
    private readonly Mock<IReadingPostRepository> _readingPostRepository = new();
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly CreateReadingPostCommandHandler _handler;

    public CreateReadingPostCommandHandlerTests()
    {
        _handler = new CreateReadingPostCommandHandler(
            _readingPostRepository.Object,
            _userBookRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_PercentageProgressProvided_CreatesPostAndUpdatesUserBookProgress()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        var command = CreateReadingPostCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithProgress(75, ProgressType.Percentage)
            .WithIsSpoiler(true)
            .Build();
        ReadingProgress? addedPost = null;

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _readingPostRepository
            .Setup(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()))
            .Callback<ReadingProgress, CancellationToken>((post, _) => addedPost = post)
            .Returns(Task.CompletedTask);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedPost);
        Assert.Equal(command.Content, addedPost.Content);
        Assert.True(addedPost.IsSpoiler);
        Assert.Equal(75, addedPost.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, addedPost.CurrentProgress?.Type);
        Assert.Equal(75, userBook.CurrentProgress?.Value);
        Assert.Equal(75, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
        Assert.True(response.IsSpoiler);
        Assert.Equal(command.ReadingDate, response.ReadingDate);
        _readingPostRepository.Verify(r => r.AddAsync(addedPost, It.IsAny<CancellationToken>()), Times.Once);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageProgressEqualsPageCount_StoresCompletedProgress()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        var command = CreateReadingPostCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithProgress(464, ProgressType.Page)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(userBook.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: userBook.BookId, pageCount: 464));
        _readingPostRepository
            .Setup(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(100, userBook.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, userBook.CurrentProgress?.Type);
        Assert.Equal(100, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
    }

    [Fact]
    public async Task Handle_PageProgressExceedsPageCount_ThrowsDomainException()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        var command = CreateReadingPostCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithProgress(465, ProgressType.Page)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(userBook.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: userBook.BookId, pageCount: 464));

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _readingPostRepository.Verify(
            r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userBookRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbiddenException()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var command = CreateReadingPostCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _readingPostRepository.Verify(
            r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
