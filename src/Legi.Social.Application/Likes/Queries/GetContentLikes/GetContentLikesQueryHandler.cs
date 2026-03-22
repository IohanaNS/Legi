using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Likes.Queries.GetContentLikes;

public class GetContentLikesQueryHandler(ILikeReadRepository likeReadRepository)
    : IRequestHandler<GetContentLikesQuery, PaginatedList<LikeUserDto>>
{
    public async Task<PaginatedList<LikeUserDto>> Handle(
        GetContentLikesQuery request,
        CancellationToken cancellationToken)
    {
        return await likeReadRepository.GetByTargetAsync(
            request.TargetType,
            request.TargetId,
            request.ViewerUserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
