using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.SearchPublicLists;

public record SearchPublicListsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<UserListSummaryDto>>;
