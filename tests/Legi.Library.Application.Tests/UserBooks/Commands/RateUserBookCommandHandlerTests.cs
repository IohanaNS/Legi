using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.RateUserBook;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Commands;

public class RateUserBookCommandHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly RateUserBookCommandHandler _handler;

    public RateUserBookCommandHandlerTests()
    {
        _handler = new RateUserBookCommandHandler(_userBookRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsBook_RatesUserBookAndPersists()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var command = RateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithStars(4.5m)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(9, userBook.CurrentRating?.Value);
        Assert.Equal(4.5m, response.Stars);
        Assert.Equal(userBook.Id, response.UserBookId);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbiddenException()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var command = RateUserBookCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userBookRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
