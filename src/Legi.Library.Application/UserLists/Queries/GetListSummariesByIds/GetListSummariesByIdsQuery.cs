using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListSummariesByIds;

public record GetListSummariesByIdsQuery(
    IReadOnlyList<Guid> ListIds) : IRequest<IReadOnlyList<UserListSummaryDto>>;
