using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.Entities;

public class ReadingProgressReviewTests
{
    private static ReadingProgress CreateValidReview(
        string? content = "A genuinely thoughtful and detailed review.",
        bool isSpoiler = false)
        => ReadingProgress.CreateReview(
            LibraryTestIds.UserBookId,
            LibraryTestIds.UserId,
            LibraryTestIds.BookId,
            LibraryTestIds.WorkId,
            content!,
            RatingFactory.Create(8),
            isSpoiler);

    [Fact]
    public void CreateReview_Valid_SetsReviewFieldsAndRaisesReviewCreatedEvent()
    {
        var review = CreateValidReview();

        Assert.True(review.IsReview);
        Assert.Null(review.CurrentProgress);
        Assert.Equal(8, review.Rating?.Value);
        Assert.Equal(LibraryTestIds.BookId, review.BookId);

        var domainEvent = Assert.IsType<ReviewCreatedDomainEvent>(
            Assert.Single(review.DomainEvents));
        Assert.Equal(review.Id, domainEvent.ReviewId);
        Assert.Equal(review.Content, domainEvent.Content);
        Assert.Equal(8, domainEvent.Stars);
        Assert.False(domainEvent.IsSpoiler);
    }

    [Fact]
    public void CreateReview_Spoiler_RaisesEventWithSpoilerFlag()
    {
        var review = CreateValidReview(isSpoiler: true);

        Assert.True(review.IsSpoiler);
        var domainEvent = Assert.IsType<ReviewCreatedDomainEvent>(
            Assert.Single(review.DomainEvents));
        Assert.True(domainEvent.IsSpoiler);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateReview_EmptyContent_Throws(string content)
    {
        Assert.Throws<DomainException>(() => CreateValidReview(content: content));
    }

    [Fact]
    public void CreateReview_ContentBelowMinimum_Throws()
    {
        var tooShort = new string('a', ReadingProgress.MinReviewContentLength - 1);
        Assert.Throws<DomainException>(() => CreateValidReview(content: tooShort));
    }

    [Fact]
    public void Delete_Review_RaisesDeletedEventFlaggedAsReview()
    {
        var review = CreateValidReview();
        review.ClearDomainEvents();

        review.Delete();

        var domainEvent = Assert.IsType<ReadingPostDeletedDomainEvent>(
            Assert.Single(review.DomainEvents));
        Assert.True(domainEvent.IsReview);
        Assert.Equal(review.Id, domainEvent.ReadingPostId);
    }
}
