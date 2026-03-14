using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListBooks;

public class GetListBooksQueryHandler
    : IRequestHandler<GetListBooksQuery, PaginatedList<UserListBookDto>>
{
    private readonly IUserListReadRepository _readRepository;

    public GetListBooksQueryHandler(IUserListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PaginatedList<UserListBookDto>> Handle(
        GetListBooksQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.GetListBooksAsync(
            request.ListId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
