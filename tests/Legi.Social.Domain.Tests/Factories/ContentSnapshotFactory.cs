using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Tests.Factories;

public static class ContentSnapshotFactory
{
    public static ContentSnapshot Create(
        InteractableType targetType = InteractableType.Post,
        Guid? targetId = null,
        Guid? ownerId = null,
        string ownerUsername = "owner",
        string? ownerAvatarUrl = "https://cdn.example.com/owner.png",
        string? contentPreview = "A progress note")
    {
        return ContentSnapshot.Create(
            targetType,
            targetId ?? SocialTestIds.TargetId,
            ownerId ?? SocialTestIds.UserId,
            ownerUsername,
            ownerAvatarUrl,
            "Dune",
            "Frank Herbert",
            "https://cdn.example.com/dune.png",
            contentPreview);
    }
}
