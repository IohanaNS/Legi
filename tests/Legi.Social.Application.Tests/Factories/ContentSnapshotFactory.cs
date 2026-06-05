using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Tests.Factories;

public static class ContentSnapshotFactory
{
    public static ContentSnapshot Create(
        InteractableType targetType = InteractableType.Post,
        Guid? targetId = null,
        Guid? ownerId = null,
        string ownerUsername = "owner",
        string? ownerAvatarUrl = "https://cdn.example.com/owner.png",
        string? bookTitle = "Dune",
        string? bookAuthor = "Frank Herbert",
        string? bookCoverUrl = "https://cdn.example.com/dune.png",
        string? contentPreview = "Progress update")
    {
        return ContentSnapshot.Create(
            targetType,
            targetId ?? Guid.NewGuid(),
            ownerId ?? Guid.NewGuid(),
            ownerUsername,
            ownerAvatarUrl,
            bookTitle,
            bookAuthor,
            bookCoverUrl,
            contentPreview);
    }
}
