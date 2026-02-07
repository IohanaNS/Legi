using Legi.Identity.Application.Common.Exceptions;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(IUserRepository userRepository) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User", request.UserId);

        // Add domain event for deletion
        user.AddDomainEvent(new UserDeletedDomainEvent(user.Id));

        // Delete user (cascade will delete refresh tokens)
        await userRepository.DeleteAsync(user, cancellationToken);
    }
}
