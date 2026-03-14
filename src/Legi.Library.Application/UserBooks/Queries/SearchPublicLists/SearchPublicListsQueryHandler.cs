using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.SearchPublicLists;

public class SearchPublicListsQueryHandler
    : IRequestHandler<SearchPublicListsQuery, PaginatedList<UserListSummaryDto>>
{
    private readonly IUserListReadRepository _readRepository;

    public SearchPublicListsQueryHandler(IUserListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PaginatedList<UserListSummaryDto>> Handle(
        SearchPublicListsQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.SearchPublicAsync(
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}