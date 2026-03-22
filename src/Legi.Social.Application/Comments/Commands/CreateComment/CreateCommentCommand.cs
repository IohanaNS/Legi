using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Comments.Commands.CreateComment;

public record CreateCommentCommand(
    Guid UserId,
    InteractableType TargetType,
    Guid TargetId,
    string Content) : IRequest<CreateCommentResponse>;