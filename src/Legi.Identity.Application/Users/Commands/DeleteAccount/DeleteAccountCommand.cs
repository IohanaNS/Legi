using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest;
