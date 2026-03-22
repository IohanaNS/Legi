using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Likes.Queries.GetContentLikes;

public record GetContentLikesQuery(
    InteractableType TargetType,
    Guid TargetId,
    Guid? ViewerUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<LikeUserDto>>;
