using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.RemoveUserBookRating;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Commands;

public class RemoveUserBookRatingCommandHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly RemoveUserBookRatingCommandHandler _handler;

    public RemoveUserBookRatingCommandHandlerTests()
    {
        _handler = new RemoveUserBookRatingCommandHandler(_userBookRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsBook_RemovesRatingAndPersists()
    {
        var userBook = UserBookBuilder.Valid()
            .WithRating(Rating.Create(8))
            .Build();
        var command = RemoveUserBookRatingCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Null(userBook.CurrentRating);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbiddenException()
    {
        var userBook = UserBookBuilder.Valid()
            .WithRating(Rating.Create(8))
            .Build();
        var command = RemoveUserBookRatingCommandBuilder.Valid()
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
