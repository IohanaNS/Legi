using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.CreateUsernameChangeChallenge;

public record CreateUsernameChangeChallengeCommand(
    Guid UserId,
    string? Password,
    string? MfaCode,
    string? RemoteIpAddress) : IRequest<UsernameChangeChallengeResponse>;

public record UsernameChangeChallengeResponse(string ChallengeToken);
