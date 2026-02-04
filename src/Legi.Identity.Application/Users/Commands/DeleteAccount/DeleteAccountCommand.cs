using Legi.Identity.Application.Common.Mediator;

namespace Legi.Identity.Application.Users.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest;
