using Legi.Identity.Application.Auth.Commands.CompleteMfaLogin;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class CompleteMfaLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IMfaSecretProtector> _secretProtectorMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();

    private CompleteMfaLoginCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _jwtMock.Object,
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

    private void SetupTokens()
    {
        _jwtMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("access", DateTime.UtcNow.AddMinutes(15)));
        _jwtMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh");
        _jwtMock.Setup(x => x.HashRefreshToken("refresh")).Returns("rhash");
        _jwtMock.Setup(x => x.GetRefreshTokenExpiresAt()).Returns(DateTime.UtcNow.AddDays(7));
    }

    [Fact]
    public async Task Handle_IssuesTokens_WithValidTotpCode()
    {
        var user = EnabledUser();
        _jwtMock.Setup(x => x.ValidateMfaChallengeToken("tok")).Returns(user.Id);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "123456", null)).Returns(true);
        SetupTokens();

        var result = await CreateHandler().Handle(
            new CompleteMfaLoginCommand("tok", "123456", "1.2.3.4"), CancellationToken.None);

        Assert.False(result.MfaRequired);
        Assert.Equal("access", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Single(user.RefreshTokens);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.LoginSucceeded)), Times.Once);
    }

    [Fact]
    public async Task Handle_IssuesTokens_AndAuditsRecoveryUse_WithValidRecoveryCode()
    {
        var user = EnabledUser("rec-hash");
        _jwtMock.Setup(x => x.ValidateMfaChallengeToken("tok")).Returns(user.Id);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(false);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("rec-hash");
        SetupTokens();

        var result = await CreateHandler().Handle(
            new CompleteMfaLoginCommand("tok", "ABCDE-FGHJK", null), CancellationToken.None);

        Assert.Equal("access", result.Token);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.RecoveryCodeUsed)), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_AndAuditsFailure_WhenCodeInvalid()
    {
        var user = EnabledUser("rec-hash");
        _jwtMock.Setup(x => x.ValidateMfaChallengeToken("tok")).Returns(user.Id);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(false);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("no-match");

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new CompleteMfaLoginCommand("tok", "000000", null), CancellationToken.None));

        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.MfaChallengeFailed)), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenChallengeTokenInvalid()
    {
        _jwtMock.Setup(x => x.ValidateMfaChallengeToken("bad")).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new CompleteMfaLoginCommand("bad", "123456", null), CancellationToken.None));

        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNoLongerHasMfaEnabled()
    {
        var user = UserFactory.Create(); // MFA not enabled
        _jwtMock.Setup(x => x.ValidateMfaChallengeToken("tok")).Returns(user.Id);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new CompleteMfaLoginCommand("tok", "123456", null), CancellationToken.None));
    }
}
