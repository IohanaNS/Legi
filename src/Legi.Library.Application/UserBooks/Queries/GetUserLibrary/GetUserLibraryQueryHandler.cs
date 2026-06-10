using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetUserLibrary;

public class GetUserLibraryQueryHandler
    : IRequestHandler<GetUserLibraryQuery, PaginatedList<UserBookDto>>
{
    private readonly IUserBookReadRepository _readRepository;

    public GetUserLibraryQueryHandler(IUserBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PaginatedList<UserBookDto>> Handle(
        GetUserLibraryQuery request,
        CancellationToken cancellationToken)
    {
        // Visibility/block rules would be enforced here (using ViewerUserId) before
        // querying — returning an empty page or throwing ForbiddenException.
        return await _readRepository.GetByUserIdAsync(
            request.TargetUserId,
            request.StatusFilter,
            wishlistFilter: null,
            search: null,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
