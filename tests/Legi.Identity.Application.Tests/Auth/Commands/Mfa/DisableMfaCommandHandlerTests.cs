using FluentValidation;
using Legi.Identity.Application.Auth.Commands.DisableMfa;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Mfa;

public class DisableMfaCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IMfaSecretProtector> _secretProtectorMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();

    private DisableMfaCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _totpServiceMock.Object,
        _secretProtectorMock.Object,
        _tokenFactoryMock.Object,
        _auditLoggerMock.Object);

    private static User EnabledUser(string recoveryHash = "rec-hash")
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        user.ConfirmMfaEnrollment([recoveryHash], DateTime.UtcNow);
        return user;
    }

    [Fact]
    public async Task Handle_DisablesMfa_WithValidTotpCode_AndAudits()
    {
        var user = EnabledUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "123456", null)).Returns(true);

        await CreateHandler().Handle(new DisableMfaCommand(user.Id, "123456"), CancellationToken.None);

        Assert.False(user.MfaEnabled);
        Assert.Null(user.TotpSecret);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.MfaDisabled && e.UserId == user.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_DisablesMfa_WithValidRecoveryCode()
    {
        var user = EnabledUser("rec-hash");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(false);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("rec-hash"); // matches stored code

        await CreateHandler().Handle(new DisableMfaCommand(user.Id, "ABCDE-FGHJK"), CancellationToken.None);

        Assert.False(user.MfaEnabled);
    }

    [Fact]
    public async Task Handle_Throws_WhenCodeInvalid()
    {
        var user = EnabledUser("rec-hash");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(false);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("different-hash");

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new DisableMfaCommand(user.Id, "wrong"), CancellationToken.None));
        Assert.True(user.MfaEnabled); // unchanged
    }

    [Fact]
    public async Task Handle_Throws_WhenMfaNotEnabled()
    {
        var user = UserFactory.Create();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new DisableMfaCommand(user.Id, "123456"), CancellationToken.None));
    }
}
