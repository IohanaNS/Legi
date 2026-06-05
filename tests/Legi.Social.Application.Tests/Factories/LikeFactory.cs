using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Tests.Factories;

public static class LikeFactory
{
    public static Like Create(
        Guid? userId = null,
        InteractableType targetType = InteractableType.Post,
        Guid? targetId = null)
    {
        return Like.Create(
            userId ?? Guid.NewGuid(),
            targetType,
            targetId ?? Guid.NewGuid());
    }
}
