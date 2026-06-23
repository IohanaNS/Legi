using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.SendAccountDeletionEmailCode;

public record SendAccountDeletionEmailCodeCommand(
    Guid UserId,
    string? Language,
    string? RemoteIpAddress) : IRequest<Unit>;
