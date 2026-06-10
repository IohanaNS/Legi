using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListBooks;

public class GetListBooksQueryHandler(
    IUserListReadRepository readRepository,
    IUserListVisibilityPolicy visibilityPolicy)
    : IRequestHandler<GetListBooksQuery, PaginatedList<UserListBookDto>>
{
    public async Task<PaginatedList<UserListBookDto>> Handle(
        GetListBooksQuery request,
        CancellationToken cancellationToken)
    {
        var list = await readRepository.GetDetailByIdAsync(
            request.ListId,
            cancellationToken);

        if (list is null)
            throw new NotFoundException("UserList", request.ListId);

        if (!visibilityPolicy.CanView(list.UserId, list.IsPublic, request.ViewerUserId))
            throw new NotFoundException("UserList", request.ListId);

        return await readRepository.GetListBooksAsync(
            request.ListId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
