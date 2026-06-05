using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.Tests.Factories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.Entities;

public class ReadingProgressTests
{
    [Fact]
    public void Create_ContentAndProgress_CreatesPostAndRaisesCreatedEvent()
    {
        var post = ReadingProgressBuilder.Valid()
            .KeepingDomainEvents()
            .Build();

        Assert.NotEqual(Guid.Empty, post.Id);
        Assert.Equal(LibraryTestIds.UserBookId, post.UserBookId);
        Assert.Equal(LibraryTestIds.UserId, post.UserId);
        Assert.Equal(LibraryTestIds.BookId, post.BookId);
        Assert.Equal("Halfway through, still engaged.", post.Content);
        Assert.Equal(50, post.CurrentProgress?.Value);
        Assert.Equal(new DateOnly(2026, 1, 15), post.ReadingDate);

        var domainEvent = Assert.IsType<ReadingProgressCreatedDomainEvent>(
            Assert.Single(post.DomainEvents));
        Assert.Equal(post.Id, domainEvent.ReadingPostId);
        Assert.Equal(post.Content, domainEvent.Content);
        Assert.Equal(50, domainEvent.ProgressValue);
        Assert.Equal("Percentage", domainEvent.ProgressType);
    }

    [Fact]
    public void Create_ProgressOnly_CreatesPost()
    {
        var post = ReadingProgressBuilder.Valid()
            .WithoutContent()
            .WithProgress(ProgressFactory.Page(42))
            .Build();

        Assert.Null(post.Content);
        Assert.Equal(42, post.CurrentProgress?.Value);
        Assert.Equal(ProgressType.Page, post.CurrentProgress?.Type);
    }

    [Fact]
    public void Create_ContentAndProgressMissing_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ReadingProgressBuilder.Valid()
                .WithoutContent()
                .WithoutProgress()
                .Build());
    }

    [Fact]
    public void Create_ContentTooLong_ThrowsDomainException()
    {
        var content = new string('a', ReadingProgress.MaxContentLength + 1);

        Assert.Throws<DomainException>(() =>
            ReadingProgressBuilder.Valid()
                .WithContent(content)
                .Build());
    }

    [Fact]
    public void Update_ValidData_UpdatesContentAndProgress()
    {
        var post = ReadingProgressBuilder.Valid().Build();
        var progress = Progress.CreatePage(200);

        post.Update("New note", progress);

        Assert.Equal("New note", post.Content);
        Assert.Equal(progress, post.CurrentProgress);
    }

    [Fact]
    public void Update_ContentAndProgressMissing_ThrowsDomainException()
    {
        var post = ReadingProgressBuilder.Valid().Build();

        Assert.Throws<DomainException>(() => post.Update(null, null));
    }

    [Fact]
    public void SocialCounters_DecrementAtZero_StayAtZero()
    {
        var post = ReadingProgressBuilder.Valid().Build();

        post.DecrementLikes();
        post.DecrementComments();

        Assert.Equal(0, post.LikesCount);
        Assert.Equal(0, post.CommentsCount);
    }

    [Fact]
    public void SocialCounters_IncrementThenDecrement_ReturnToZero()
    {
        var post = ReadingProgressBuilder.Valid().Build();

        post.IncrementLikes();
        post.IncrementComments();
        post.DecrementLikes();
        post.DecrementComments();

        Assert.Equal(0, post.LikesCount);
        Assert.Equal(0, post.CommentsCount);
    }

    [Fact]
    public void Delete_Always_RaisesReadingPostDeletedEvent()
    {
        var post = ReadingProgressBuilder.Valid().Build();

        post.Delete();

        var domainEvent = Assert.IsType<ReadingPostDeletedDomainEvent>(
            Assert.Single(post.DomainEvents));
        Assert.Equal(post.Id, domainEvent.ReadingPostId);
        Assert.Equal(post.UserId, domainEvent.UserId);
        Assert.Equal(post.BookId, domainEvent.BookId);
    }
}
