using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class NotificationTests
{
    [Fact]
    public void CreateLike_SetsFields_AndStartsUnread()
    {
        var recipientId = SocialTestIds.UserId;
        var actorId = SocialTestIds.OtherUserId;

        var notification = Notification.CreateLike(
            recipientId,
            actorId,
            "carlos",
            "https://cdn.example.com/carlos.png",
            InteractableType.Review,
            SocialTestIds.TargetId);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(recipientId, notification.RecipientId);
        Assert.Equal(actorId, notification.ActorId);
        Assert.Equal("carlos", notification.ActorUsername);
        Assert.Equal("https://cdn.example.com/carlos.png", notification.ActorAvatarUrl);
        Assert.Equal(NotificationType.Like, notification.NotificationType);
        Assert.Equal(InteractableType.Review, notification.TargetType);
        Assert.Equal(SocialTestIds.TargetId, notification.TargetId);
        Assert.Null(notification.CommentPreview);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public void CreateComment_SetsCommentTypeAndPreview()
    {
        var notification = Notification.CreateComment(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId,
            "carlos",
            null,
            InteractableType.Post,
            SocialTestIds.TargetId,
            "Great review!");

        Assert.Equal(NotificationType.Comment, notification.NotificationType);
        Assert.Equal("Great review!", notification.CommentPreview);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void CreateComment_TruncatesPreview_ToMaxLength()
    {
        var longText = new string('a', Notification.MaxCommentPreviewLength + 50);

        var notification = Notification.CreateComment(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId,
            "carlos",
            null,
            InteractableType.List,
            SocialTestIds.TargetId,
            longText);

        Assert.Equal(Notification.MaxCommentPreviewLength, notification.CommentPreview!.Length);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndReadAt()
    {
        var notification = Notification.CreateLike(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId,
            "carlos",
            null,
            InteractableType.Post,
            SocialTestIds.TargetId);

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public void MarkAsRead_IsIdempotent_WhenAlreadyRead()
    {
        var notification = Notification.CreateLike(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId,
            "carlos",
            null,
            InteractableType.Post,
            SocialTestIds.TargetId);

        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead();

        Assert.Equal(firstReadAt, notification.ReadAt);
    }
}
