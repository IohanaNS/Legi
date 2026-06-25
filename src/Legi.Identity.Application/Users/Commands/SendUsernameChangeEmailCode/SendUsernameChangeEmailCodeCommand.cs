using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.SendUsernameChangeEmailCode;

public record SendUsernameChangeEmailCodeCommand(
    Guid UserId,
    string? Language,
    string? RemoteIpAddress) : IRequest<Unit>;
