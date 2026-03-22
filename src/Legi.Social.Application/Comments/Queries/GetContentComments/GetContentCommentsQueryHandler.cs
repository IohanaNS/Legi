using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Comments.Queries.GetContentComments;

public class GetContentCommentsQueryHandler(ICommentReadRepository commentReadRepository)
    : IRequestHandler<GetContentCommentsQuery, PaginatedList<CommentDto>>
{
    public async Task<PaginatedList<CommentDto>> Handle(
        GetContentCommentsQuery request,
        CancellationToken cancellationToken)
    {
        return await commentReadRepository.GetByTargetAsync(
            request.TargetType,
            request.TargetId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}