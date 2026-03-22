using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Comments.Commands.DeleteComment;

/// <summary>
/// ActorId is the user performing the deletion — could be the comment author
/// OR the owner of the content being commented on.
/// </summary>
public record DeleteCommentCommand(Guid ActorId, Guid CommentId) : IRequest;