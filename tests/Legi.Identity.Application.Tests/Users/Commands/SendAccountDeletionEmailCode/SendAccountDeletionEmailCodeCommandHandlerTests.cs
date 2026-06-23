using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.SendAccountDeletionEmailCode;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Commands.SendAccountDeletionEmailCode;

public class SendAccountDeletionEmailCodeCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMfaEmailCodeRepository> _codeRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly MfaSettings _mfaSettings = new() { EmailCodeLifetimeMinutes = 10 };

    private SendAccountDeletionEmailCodeCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _codeRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _emailSenderMock.Object,
        _mfaSettings);

    [Fact]
    public async Task Handle_ShouldIssueAndSendEmailCode_WhenUserUsesEmailMfa()
    {
        var user = UserFactory.Create();
        user.EnableEmailMfa(["rec"], DateTime.UtcNow);
        var command = new SendAccountDeletionEmailCodeCommand(user.Id, "en", "1.2.3.4");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");

        await CreateHandler().Handle(command, CancellationToken.None);

        _codeRepositoryMock.Verify(
            x => x.IssueAsync(It.Is<MfaEmailCode>(c => c.UserId == user.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSenderMock.Verify(
            x => x.SendAsync(user.Email.Value, It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenUserDoesNotUseEmailMfa()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(["rec"], DateTime.UtcNow);
        var command = new SendAccountDeletionEmailCodeCommand(user.Id, "en", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
