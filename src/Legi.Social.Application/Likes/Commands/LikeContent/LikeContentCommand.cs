using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Likes.Commands.LikeContent;

public record LikeContentCommand(
    Guid UserId,
    InteractableType TargetType,
    Guid TargetId) : IRequest<LikeResponse>;
