using Legi.SharedKernel;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class CommentTests
{
    [Fact]
    public void Create_ValidData_CreatesCommentAndRaisesCreatedEvent()
    {
        var comment = Comment.Create(
            SocialTestIds.UserId,
            InteractableType.Post,
            SocialTestIds.TargetId,
            "Loved this update");

        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(SocialTestIds.UserId, comment.UserId);
        Assert.Equal(InteractableType.Post, comment.TargetType);
        Assert.Equal(SocialTestIds.TargetId, comment.TargetId);
        Assert.Equal("Loved this update", comment.Content);

        var domainEvent = Assert.IsType<CommentCreatedDomainEvent>(
            Assert.Single(comment.DomainEvents));
        Assert.Equal(comment.Id, domainEvent.CommentId);
        Assert.Equal(comment.UserId, domainEvent.UserId);
        Assert.Equal(comment.TargetType, domainEvent.TargetType);
        Assert.Equal(comment.TargetId, domainEvent.TargetId);
    }

    [Fact]
    public void Create_ContentIsWhitespace_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Comment.Create(
                SocialTestIds.UserId,
                InteractableType.Post,
                SocialTestIds.TargetId,
                "   "));
    }

    [Fact]
    public void Create_ContentExceedsMaximumLength_ThrowsDomainException()
    {
        var content = new string('a', Comment.MaxContentLength + 1);

        Assert.Throws<DomainException>(() =>
            Comment.Create(
                SocialTestIds.UserId,
                InteractableType.Post,
                SocialTestIds.TargetId,
                content));
    }

    [Fact]
    public void MarkForDeletion_ExistingComment_RaisesDeletedEvent()
    {
        var comment = Comment.Create(
            SocialTestIds.UserId,
            InteractableType.List,
            SocialTestIds.TargetId,
            "Useful list");
        comment.ClearDomainEvents();

        comment.MarkForDeletion();

        var domainEvent = Assert.IsType<CommentDeletedDomainEvent>(
            Assert.Single(comment.DomainEvents));
        Assert.Equal(comment.Id, domainEvent.Id);
        Assert.Equal(comment.UserId, domainEvent.UserId);
        Assert.Equal(comment.TargetType, domainEvent.TargetType);
        Assert.Equal(comment.TargetId, domainEvent.TargetId);
    }
}
