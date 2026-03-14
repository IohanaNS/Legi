using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetMyLibrary;

public class GetMyLibraryQueryHandler
    : IRequestHandler<GetMyLibraryQuery, PaginatedList<UserBookDto>>
{
    private readonly IUserBookReadRepository _readRepository;

    public GetMyLibraryQueryHandler(IUserBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PaginatedList<UserBookDto>> Handle(
        GetMyLibraryQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.GetByUserIdAsync(
            request.UserId,
            request.StatusFilter,
            request.WishlistFilter,
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
