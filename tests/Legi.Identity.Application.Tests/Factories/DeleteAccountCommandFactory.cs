using Legi.Identity.Application.Users.Commands.DeleteAccount;

namespace Legi.Identity.Application.Tests.Factories;

public static class DeleteAccountCommandFactory
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static DeleteAccountCommand Create(Guid? userId = null)
    {
        return new DeleteAccountCommand(userId ?? DefaultUserId);
    }
}
