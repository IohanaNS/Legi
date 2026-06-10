using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListDetails;

public class GetListDetailsQueryHandler(
    IUserListReadRepository readRepository,
    IUserListVisibilityPolicy visibilityPolicy)
    : IRequestHandler<GetListDetailsQuery, UserListDetailDto>
{
    public async Task<UserListDetailDto> Handle(
        GetListDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await readRepository.GetDetailByIdAsync(
            request.ListId,
            cancellationToken);

        if (list is null)
            throw new NotFoundException("UserList", request.ListId);

        if (!visibilityPolicy.CanView(list.UserId, list.IsPublic, request.ViewerUserId))
            throw new NotFoundException("UserList", request.ListId);

        return list;
    }
}
