using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.UpdateUserList;

public class UpdateUserListCommandHandler
    : IRequestHandler<UpdateUserListCommand, UpdateUserListResponse>
{
    private readonly IUserListRepository _userListRepository;

    public UpdateUserListCommandHandler(IUserListRepository userListRepository)
    {
        _userListRepository = userListRepository;
    }

    public async Task<UpdateUserListResponse> Handle(
        UpdateUserListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        // Check name uniqueness only if name changed
        if (!string.Equals(list.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _userListRepository.ExistsByUserAndNameAsync(
                request.UserId, request.Name, cancellationToken);

            if (nameExists)
                throw new ConflictException($"You already have a list named '{request.Name}'.");
        }

        list.UpdateDetails(request.Name, request.Description, request.IsPublic);

        await _userListRepository.UpdateAsync(list, cancellationToken);

        return new UpdateUserListResponse(
            list.Id,
            list.Name,
            list.Description,
            list.IsPublic,
            list.UpdatedAt);
    }
}