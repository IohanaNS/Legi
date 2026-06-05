using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Tests.Factories;

public static class CommentFactory
{
    public static Comment Create(
        Guid? userId = null,
        InteractableType targetType = InteractableType.Post,
        Guid? targetId = null,
        string content = "Great update")
    {
        return Comment.Create(
            userId ?? Guid.NewGuid(),
            targetType,
            targetId ?? Guid.NewGuid(),
            content);
    }
}
