using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetMyLists;

public class GetMyListsQueryHandler
    : IRequestHandler<GetMyListsQuery, IReadOnlyList<UserListSummaryDto>>
{
    private readonly IUserListReadRepository _readRepository;

    public GetMyListsQueryHandler(IUserListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IReadOnlyList<UserListSummaryDto>> Handle(
        GetMyListsQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);
    }
}
