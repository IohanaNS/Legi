using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
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
        var list = await _readRepository.GetDetailByIdAsync(
            request.ListId,
            cancellationToken);

        if (list is null)
            throw new NotFoundException("UserList", request.ListId);

        if (!list.IsPublic && list.UserId != request.ViewerUserId)
            throw new NotFoundException("UserList", request.ListId);

        return await _readRepository.GetListBooksAsync(
            request.ListId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
