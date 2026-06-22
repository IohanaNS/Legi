using FluentValidation;
using Legi.Identity.Application.Auth.Commands.ConfirmMfaSetup;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Mfa;

public class ConfirmMfaSetupCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IMfaSecretProtector> _secretProtectorMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();

    private ConfirmMfaSetupCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _totpServiceMock.Object,
        _secretProtectorMock.Object,
        _tokenFactoryMock.Object,
        _auditLoggerMock.Object);

    [Fact]
    public async Task Handle_EnablesMfa_StoresRecoveryCodes_AndAudits()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "123456", null)).Returns(true);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"hash:{s}");

        var result = await CreateHandler().Handle(
            new ConfirmMfaSetupCommand(user.Id, "123456"), CancellationToken.None);

        Assert.True(user.MfaEnabled);
        Assert.Equal(MfaRecoveryCodeGenerator.Count, result.RecoveryCodes.Count);
        Assert.Equal(MfaRecoveryCodeGenerator.Count, user.MfaRecoveryCodes.Count);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.MfaEnabled && e.UserId == user.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenCodeInvalid()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "000000", null)).Returns(false);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new ConfirmMfaSetupCommand(user.Id, "000000"), CancellationToken.None));
        Assert.False(user.MfaEnabled);
    }

    [Fact]
    public async Task Handle_Throws_WhenNoEnrollmentStarted()
    {
        var user = UserFactory.Create(); // no StartMfaEnrollment
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new ConfirmMfaSetupCommand(user.Id, "123456"), CancellationToken.None));
    }
}
