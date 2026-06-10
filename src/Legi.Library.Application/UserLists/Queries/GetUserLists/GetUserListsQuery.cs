using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetUserLists;

public record GetUserListsQuery(
    Guid TargetUserId,
    Guid ViewerUserId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<UserListSummaryDto>>;
