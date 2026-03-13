using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.CreateUserList;

public class CreateUserListCommandHandler : IRequestHandler<CreateUserListCommand, CreateUserListResponse>
{
    private const int MaxListsPerUser = 100;
    private readonly IUserListRepository _userListRepository;

    public CreateUserListCommandHandler(IUserListRepository userListRepository)
    {
        _userListRepository = userListRepository;
    }

    public async Task<CreateUserListResponse> Handle(CreateUserListCommand request, CancellationToken cancellationToken)
    {
        var listCount = await _userListRepository.GetCountByUserIdAsync(
            request.UserId, cancellationToken);

        if (listCount >= MaxListsPerUser)
            throw new DomainException($"You cannot have more than {MaxListsPerUser} lists.");

        var nameExists = await _userListRepository.ExistsByUserAndNameAsync(
            request.UserId, request.Name, cancellationToken);

        if (nameExists)
            throw new ConflictException($"You already have a list named '{request.Name}'.");

        var list = UserList.Create(
            request.UserId,
            request.Name,
            request.Description,
            request.IsPublic);

        // 4. Persist
        await _userListRepository.AddAsync(list, cancellationToken);

        return new CreateUserListResponse(
            list.Id,
            list.Name,
            list.Description,
            list.IsPublic,
            list.CreatedAt);
    }
}