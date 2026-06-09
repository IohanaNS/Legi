using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.Tests.Factories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.Entities;

public class UserBookTests
{
    [Fact]
    public void Create_ValidData_CreatesNotStartedBookAndRaisesAddedEvent()
    {
        var userBook = UserBookFactory.Create(wishList: true);

        Assert.NotEqual(Guid.Empty, userBook.Id);
        Assert.Equal(LibraryTestIds.UserId, userBook.UserId);
        Assert.Equal(LibraryTestIds.BookId, userBook.BookId);
        Assert.Equal(ReadingStatus.NotStarted, userBook.Status);
        Assert.True(userBook.WishList);

        var domainEvent = Assert.IsType<BookAddedToLibraryDomainEvent>(
            Assert.Single(userBook.DomainEvents));
        Assert.Equal(userBook.Id, domainEvent.UserBookId);
        Assert.Equal(userBook.UserId, domainEvent.UserId);
        Assert.Equal(userBook.BookId, domainEvent.BookId);
        Assert.True(domainEvent.WishList);
    }

    [Fact]
    public void ChangeReadingStatus_NewStatusIsSame_DoesNotRaiseDomainEvent()
    {
        var userBook = UserBookFactory.Create();
        userBook.ClearDomainEvents();

        userBook.ChangeReadingStatus(ReadingStatus.NotStarted);

        Assert.Empty(userBook.DomainEvents);
        Assert.Equal(ReadingStatus.NotStarted, userBook.Status);
    }

    [Fact]
    public void ChangeReadingStatus_ReadingStarted_RemovesBookFromWishlistAndRaisesStatusEvent()
    {
        var userBook = UserBookFactory.Create(wishList: true);
        userBook.ClearDomainEvents();

        userBook.ChangeReadingStatus(ReadingStatus.Reading);

        Assert.False(userBook.WishList);
        Assert.Equal(ReadingStatus.Reading, userBook.Status);

        var domainEvent = Assert.IsType<ReadingStatusChangedDomainEvent>(
            Assert.Single(userBook.DomainEvents));
        Assert.Equal(ReadingStatus.NotStarted, domainEvent.OldStatus);
        Assert.Equal(ReadingStatus.Reading, domainEvent.NewStatus);
    }

    [Fact]
    public void ChangeReadingStatus_Finished_SetsCompletedProgress()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();

        userBook.ChangeReadingStatus(ReadingStatus.Finished);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(Progress.MaxPercentage, userBook.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Percentage, userBook.CurrentProgress?.Type);
    }

    [Fact]
    public void ChangeReadingStatus_MovingAwayFromFinished_ClearsProgress()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Finished)
            .Build();

        userBook.ChangeReadingStatus(ReadingStatus.Reading);

        Assert.Equal(ReadingStatus.Reading, userBook.Status);
        Assert.Null(userBook.CurrentProgress);
    }

    [Fact]
    public void SetWishList_ReadingAlreadyStarted_ThrowsDomainException()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();

        Assert.Throws<DomainException>(() => userBook.SetWishList(true));
    }

    [Fact]
    public void UpdateProgress_PercentageComplete_FinishesBookAndRaisesStatusEvent()
    {
        var userBook = UserBookBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .Build();
        userBook.ClearDomainEvents();

        userBook.UpdateProgress(Progress.Completed());

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(Progress.MaxPercentage, userBook.CurrentProgress?.Value);
        Assert.IsType<ReadingStatusChangedDomainEvent>(Assert.Single(userBook.DomainEvents));
    }

    [Fact]
    public void Rate_NewRating_SetsRatingAndRaisesRatedEvent()
    {
        var userBook = UserBookFactory.Create();
        userBook.ClearDomainEvents();
        var rating = RatingFactory.Create(8);

        userBook.Rate(rating);

        Assert.Equal(rating, userBook.CurrentRating);
        var domainEvent = Assert.IsType<UserBookRatedDomainEvent>(
            Assert.Single(userBook.DomainEvents));
        Assert.Null(domainEvent.OldRating);
        Assert.Equal(rating, domainEvent.NewRating);
    }

    [Fact]
    public void RemoveRating_RatingExists_RemovesRatingAndRaisesRemovedEvent()
    {
        var userBook = UserBookBuilder.Valid()
            .WithRating(RatingFactory.Create(6))
            .Build();

        userBook.RemoveRating();

        Assert.Null(userBook.CurrentRating);
        var domainEvent = Assert.IsType<UserBookRatingRemovedDomainEvent>(
            Assert.Single(userBook.DomainEvents));
        Assert.Equal(6, domainEvent.OldRating.Value);
    }

    [Fact]
    public void RemoveRating_RatingMissing_DoesNotRaiseDomainEvent()
    {
        var userBook = UserBookFactory.Create();
        userBook.ClearDomainEvents();

        userBook.RemoveRating();

        Assert.Null(userBook.CurrentRating);
        Assert.Empty(userBook.DomainEvents);
    }

    [Fact]
    public void Remove_NotDeleted_SoftDeletesBookAndRaisesRemovedEvent()
    {
        var userBook = UserBookFactory.Create();
        userBook.ClearDomainEvents();

        userBook.Remove();

        Assert.True(userBook.IsDeleted);
        Assert.NotNull(userBook.DeletedAt);

        var domainEvent = Assert.IsType<BookRemovedFromLibraryDomainEvent>(
            Assert.Single(userBook.DomainEvents));
        Assert.Equal(userBook.Id, domainEvent.UserBookId);
        Assert.Equal(userBook.UserId, domainEvent.UserId);
        Assert.Equal(userBook.BookId, domainEvent.BookId);
    }

    [Fact]
    public void Remove_AlreadyDeleted_ThrowsDomainException()
    {
        var userBook = UserBookFactory.Create();
        userBook.Remove();

        Assert.Throws<DomainException>(() => userBook.Remove());
    }

    [Fact]
    public void ChangeReadingStatus_FinishedWithDate_SetsFinishedReadingAt()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();
        var finishedOn = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1);

        userBook.ChangeReadingStatus(ReadingStatus.Finished, finishedOn);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Equal(finishedOn, userBook.FinishedReadingAt);
    }

    [Fact]
    public void ChangeReadingStatus_FinishedWithoutDate_LeavesFinishedReadingAtNull()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();

        userBook.ChangeReadingStatus(ReadingStatus.Finished);

        Assert.Equal(ReadingStatus.Finished, userBook.Status);
        Assert.Null(userBook.FinishedReadingAt);
    }

    [Fact]
    public void ChangeReadingStatus_FutureDate_ThrowsDomainException()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

        Assert.Throws<DomainException>(
            () => userBook.ChangeReadingStatus(ReadingStatus.Finished, future));
    }

    [Fact]
    public void ChangeReadingStatus_MovingAwayFromFinished_ClearsFinishedReadingAt()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();
        userBook.ChangeReadingStatus(
            ReadingStatus.Finished, DateOnly.FromDateTime(DateTime.UtcNow));

        userBook.ChangeReadingStatus(ReadingStatus.Reading);

        Assert.Null(userBook.FinishedReadingAt);
    }

    [Fact]
    public void ChangeReadingStatus_Abandoned_DoesNotSetFinishedReadingAt()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();

        userBook.ChangeReadingStatus(ReadingStatus.Abandoned);

        Assert.Null(userBook.FinishedReadingAt);
    }

    [Fact]
    public void UpdateProgress_PercentageComplete_SetsFinishedReadingAtToToday()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();

        userBook.UpdateProgress(Progress.Completed());

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), userBook.FinishedReadingAt);
    }

    [Fact]
    public void SetFinishedReadingDate_BookFinished_UpdatesDate()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Finished).Build();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);

        userBook.SetFinishedReadingDate(newDate);

        Assert.Equal(newDate, userBook.FinishedReadingAt);
    }

    [Fact]
    public void SetFinishedReadingDate_Null_ResetsToUnknown()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Finished).Build();
        userBook.SetFinishedReadingDate(DateOnly.FromDateTime(DateTime.UtcNow));

        userBook.SetFinishedReadingDate(null);

        Assert.Null(userBook.FinishedReadingAt);
    }

    [Fact]
    public void SetFinishedReadingDate_BookNotFinished_ThrowsDomainException()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Reading).Build();

        Assert.Throws<DomainException>(
            () => userBook.SetFinishedReadingDate(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void SetFinishedReadingDate_FutureDate_ThrowsDomainException()
    {
        var userBook = UserBookBuilder.Valid().WithStatus(ReadingStatus.Finished).Build();
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

        Assert.Throws<DomainException>(() => userBook.SetFinishedReadingDate(future));
    }
}
