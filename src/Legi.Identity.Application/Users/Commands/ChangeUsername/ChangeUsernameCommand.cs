using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.ChangeUsername;

public record ChangeUsernameCommand(
    Guid UserId,
    string NewUsername,
    string ChallengeToken) : IRequest<ChangeUsernameResponse>;

public record ChangeUsernameResponse(string NewUsername);
