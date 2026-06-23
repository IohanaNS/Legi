using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;

public record CreateAccountDeletionChallengeCommand(
    Guid UserId,
    string? Password,
    string? MfaCode,
    string? RemoteIpAddress) : IRequest<AccountDeletionChallengeResponse>;

public record AccountDeletionChallengeResponse(string DeletionToken);
