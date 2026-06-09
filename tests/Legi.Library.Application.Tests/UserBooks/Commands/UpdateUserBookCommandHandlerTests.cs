using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.UpdateUserBook;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Commands;

public class UpdateUserBookCommandHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly UpdateUserBookCommandHandler _handler;

    public UpdateUserBookCommandHandlerTests()
    {
        _handler = new UpdateUserBookCommandHandler(
            _userBookRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_StatusAndPercentageProgressProvided_UpdatesUserBookAndPersists()
    {
        var userBook = UserBookBuilder.Valid()
            .AsWishlist()
            .Build();
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithStatus(ReadingStatus.Reading)
            .WithProgress(30, ProgressType.Percentage)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ReadingStatus.Reading, userBook.Status);
        Assert.False(userBook.WishList);
        Assert.Equal(30, userBook.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, userBook.CurrentProgress?.Type);
        Assert.Equal(userBook.Id, response.UserBookId);
        Assert.Equal("Reading", response.Status);
        Assert.Equal(30, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageProgressReachesPageCount_CompletesUserBook()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithProgress(464, ProgressType.Page)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(userBook.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: userBook.BookId, pageCount: 464));
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(100, userBook.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, userBook.CurrentProgress?.Type);
        Assert.Equal("Finished", response.Status);
        Assert.Equal(100, response.ProgressValue);
        Assert.Equal("Percentage", response.ProgressType);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnUserBook_ThrowsUnauthorizedAccessException()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .WithStatus(ReadingStatus.Reading)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userBookRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StatusFinishedWithDate_SetsFinishedReadingAtAndReturnsIt()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();
        var finishedOn = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithStatus(ReadingStatus.Finished)
            .WithFinishedReadingAt(finishedOn)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(finishedOn, userBook.FinishedReadingAt);
        Assert.Equal(finishedOn, response.FinishedReadingAt);
    }

    [Fact]
    public async Task Handle_AlreadyFinishedWithNewDate_EditsFinishedReadingAt()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Finished).Build();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-2);
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithStatus(ReadingStatus.Finished)
            .WithFinishedReadingAt(newDate)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(newDate, userBook.FinishedReadingAt);
        Assert.Equal(newDate, response.FinishedReadingAt);
    }

    [Fact]
    public async Task Handle_AlreadyFinishedWithNullDate_ResetsFinishedReadingAtToUnknown()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Finished).Build();
        userBook.SetFinishedReadingDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5));
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithStatus(ReadingStatus.Finished)
            .WithFinishedReadingAt(null)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Null(userBook.FinishedReadingAt);
        Assert.Null(response.FinishedReadingAt);
    }
}
