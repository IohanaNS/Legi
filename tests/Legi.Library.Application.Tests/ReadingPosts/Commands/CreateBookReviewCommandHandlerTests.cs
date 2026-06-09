using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.ReadingPosts.Commands.CreateBookReview;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.Commands;

public class CreateBookReviewCommandHandlerTests
{
    private readonly Mock<IReadingPostRepository> _readingPostRepository = new();
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly CreateBookReviewCommandHandler _handler;

    public CreateBookReviewCommandHandlerTests()
    {
        _handler = new CreateBookReviewCommandHandler(
            _readingPostRepository.Object, _userBookRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsBook_RatesAndCreatesReviewAndPersistsBoth()
    {
        var userBook = UserBookBuilder.Valid().Build();
        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        var command = new CreateBookReviewCommand(
            userBook.Id, LibraryTestIds.UserId, "A genuinely thoughtful review of the book.", 4.5m, IsSpoiler: false);

        ReadingProgress? added = null;
        _readingPostRepository
            .Setup(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()))
            .Callback<ReadingProgress, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        // Rating set on the user book, flagged as part of the review.
        Assert.Equal(9, userBook.CurrentRating?.Value);
        var ratedEvent = Assert.Single(userBook.DomainEvents.OfType<UserBookRatedDomainEvent>());
        Assert.True(ratedEvent.IsPartOfReview);

        // Review post created and persisted.
        Assert.NotNull(added);
        Assert.True(added!.IsReview);
        Assert.Equal(9, added.Rating?.Value);
        Assert.Equal(4.5m, response.Stars);

        _readingPostRepository.Verify(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbidden()
    {
        var userBook = UserBookBuilder.Valid().Build();
        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        var command = new CreateBookReviewCommand(
            userBook.Id, LibraryTestIds.OtherUserId, "A genuinely thoughtful review of the book.", 4.5m);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _readingPostRepository.Verify(
            r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserBookNotFound_ThrowsNotFound()
    {
        _userBookRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserBook?)null);

        var command = new CreateBookReviewCommand(
            Guid.NewGuid(), LibraryTestIds.UserId, "A genuinely thoughtful review of the book.", 4.5m);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
