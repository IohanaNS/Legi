using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListDetails;

public class GetListDetailsQueryHandler
    : IRequestHandler<GetListDetailsQuery, UserListDetailDto>
{
    private readonly IUserListReadRepository _readRepository;

    public GetListDetailsQueryHandler(IUserListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<UserListDetailDto> Handle(
        GetListDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _readRepository.GetDetailByIdAsync(
            request.ListId,
            cancellationToken);

        if (list is null)
            throw new NotFoundException("UserList", request.ListId);

        return list;
    }
}
