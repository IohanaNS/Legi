using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    ISecurityAuditLogger auditLogger) : IRequestHandler<DeleteAccountCommand>
{
    private const string InvalidDeletionChallengeMessage = "Invalid or expired account deletion challenge.";

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var verifiedUserId = jwtTokenService.ValidateAccountDeletionChallengeToken(request.DeletionToken);
        if (verifiedUserId != request.UserId)
        {
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.AccountDeletionChallengeFailed,
                UserId: request.UserId,
                Detail: "invalid-delete-token"));

            throw new UnauthorizedException(InvalidDeletionChallengeMessage);
        }

        // Get user
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User", request.UserId);

        // Add domain event for deletion
        user.AddDomainEvent(new UserDeletedDomainEvent(user.Id));

        // Delete user (cascade will delete refresh tokens)
        await userRepository.DeleteAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.AccountDeleted,
            UserId: user.Id));
    }
}
