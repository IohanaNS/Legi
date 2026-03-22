using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Likes.Commands.UnlikeContent;

public record UnlikeContentCommand(
    Guid UserId,
    InteractableType TargetType,
    Guid TargetId) : IRequest;
