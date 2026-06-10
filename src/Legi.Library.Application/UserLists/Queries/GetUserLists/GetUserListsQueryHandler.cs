using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetUserLists;

public class GetUserListsQueryHandler(IUserListReadRepository readRepository)
    : IRequestHandler<GetUserListsQuery, PaginatedList<UserListSummaryDto>>
{
    public async Task<PaginatedList<UserListSummaryDto>> Handle(
        GetUserListsQuery request,
        CancellationToken cancellationToken)
    {
        return await readRepository.GetVisibleByUserIdAsync(
            request.TargetUserId,
            request.ViewerUserId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
