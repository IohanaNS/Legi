using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListSummariesByIds;

public class GetListSummariesByIdsQueryHandler(IUserListReadRepository readRepository)
    : IRequestHandler<GetListSummariesByIdsQuery, IReadOnlyList<UserListSummaryDto>>
{
    public async Task<IReadOnlyList<UserListSummaryDto>> Handle(
        GetListSummariesByIdsQuery request,
        CancellationToken cancellationToken)
    {
        return await readRepository.GetPublicSummariesByIdsAsync(
            request.ListIds, cancellationToken);
    }
}
