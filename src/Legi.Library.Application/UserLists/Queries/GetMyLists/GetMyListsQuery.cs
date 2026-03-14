using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetMyLists;

public record GetMyListsQuery(
    Guid UserId
) : IRequest<IReadOnlyList<UserListSummaryDto>>;
