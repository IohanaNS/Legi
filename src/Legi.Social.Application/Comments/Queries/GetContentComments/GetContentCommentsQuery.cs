using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Comments.Queries.GetContentComments;

public record GetContentCommentsQuery(
    InteractableType TargetType,
    Guid TargetId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<CommentDto>>;