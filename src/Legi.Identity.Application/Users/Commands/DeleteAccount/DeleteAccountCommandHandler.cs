using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.Repositories;
using MediatR;

namespace Legi.Identity.Application.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteAccountCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User", request.UserId);

        // Add domain event for deletion
        user.AddDomainEvent(new UserDeletedDomainEvent(user.Id));

        // Delete user (cascade will delete refresh tokens)
        await _userRepository.DeleteAsync(user, cancellationToken);
    }
}
