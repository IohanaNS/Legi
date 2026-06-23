using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Domain.ValueObjects;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Commands.CreateAccountDeletionChallenge;

public class CreateAccountDeletionChallengeCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ILoginAttemptRepository> _accountDeletionAttemptRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IMfaSecretProtector> _secretProtectorMock = new();
    private readonly Mock<IMfaEmailCodeRepository> _emailCodeRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();
    private readonly AccountDeletionChallengeLockoutSettings _lockoutSettings = new()
    {
        MaxFailedAttempts = 3,
        FailureWindowMinutes = 15,
        LockoutDurationMinutes = 15
    };

    public CreateAccountDeletionChallengeCommandHandlerTests()
    {
        _accountDeletionAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginAttempt?)null);
    }

    private CreateAccountDeletionChallengeCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _accountDeletionAttemptRepositoryMock.Object,
        _passwordHasherMock.Object,
        _jwtTokenServiceMock.Object,
        _totpServiceMock.Object,
        _secretProtectorMock.Object,
        _emailCodeRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _lockoutSettings,
        _auditLoggerMock.Object);

    [Fact]
    public async Task Handle_ShouldIssueDeletionToken_WhenPasswordIsValid()
    {
        var user = UserFactory.Create(passwordHash: "hash");
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "correct-password", null, "1.2.3.4");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correct-password", "hash")).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccountDeletionChallengeToken(user)).Returns("delete-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("delete-token", result.DeletionToken);
        _accountDeletionAttemptRepositoryMock.Verify(
            x => x.ClearAsync(AttemptIdentifier(user.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenPasswordIsInvalid()
    {
        var user = UserFactory.Create(passwordHash: "hash");
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "wrong-password", null, "1.2.3.4");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("wrong-password", "hash")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccountDeletionChallengeToken(It.IsAny<User>()),
            Times.Never);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e =>
                e.Type == SecurityEventType.AccountDeletionChallengeFailed &&
                e.Detail == "invalid-password")),
            Times.Once);
        _accountDeletionAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                AttemptIdentifier(user.Id),
                _lockoutSettings.MaxFailedAttempts,
                _lockoutSettings.FailureWindow,
                _lockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenDeletionChallengeIsLockedOut()
    {
        var now = DateTime.UtcNow;
        var user = UserFactory.Create(passwordHash: "hash");
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "correct-password", null, "1.2.3.4");
        var attempt = LoginAttempt.Create(AttemptIdentifier(user.Id), now);
        attempt.RecordFailedLogin(
            maxFailedAttempts: 1,
            failureWindow: TimeSpan.FromMinutes(15),
            lockoutDuration: TimeSpan.FromMinutes(15),
            utcNow: now);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _accountDeletionAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(AttemptIdentifier(user.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _accountDeletionAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e =>
                e.Type == SecurityEventType.AccountDeletionChallengeFailed &&
                e.Detail == "locked-out")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRecordFailedAttempt_WhenMfaCodeIsInvalid()
    {
        var user = UserFactory.Create(passwordHash: "hash");
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(["rec"], DateTime.UtcNow);
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "correct-password", "000000", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correct-password", "hash")).Returns(true);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "000000", null)).Returns(false);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _accountDeletionAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                AttemptIdentifier(user.Id),
                _lockoutSettings.MaxFailedAttempts,
                _lockoutSettings.FailureWindow,
                _lockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e =>
                e.Type == SecurityEventType.AccountDeletionChallengeFailed &&
                e.Detail == "invalid-mfa")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIssueDeletionToken_WhenPasswordAndTotpAreValid()
    {
        var user = UserFactory.Create(passwordHash: "hash");
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(["rec"], DateTime.UtcNow);
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "correct-password", "123456", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correct-password", "hash")).Returns(true);
        _secretProtectorMock.Setup(x => x.Unprotect("enc")).Returns("SECRET");
        _totpServiceMock.Setup(x => x.VerifyCode("SECRET", "123456", null)).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccountDeletionChallengeToken(user)).Returns("delete-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("delete-token", result.DeletionToken);
    }

    [Fact]
    public async Task Handle_ShouldConsumeEmailCodeAndIssueDeletionToken_WhenEmailMfaCodeIsValid()
    {
        var now = DateTime.UtcNow;
        var user = UserFactory.Create(passwordHash: "hash");
        user.EnableEmailMfa(["rec"], now);
        var emailCode = MfaEmailCode.Issue(user.Id, "h:123456", now.AddMinutes(10), now);
        var command = new CreateAccountDeletionChallengeCommand(user.Id, "correct-password", "123456", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correct-password", "hash")).Returns(true);
        _emailCodeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailCode);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");
        _jwtTokenServiceMock.Setup(x => x.GenerateAccountDeletionChallengeToken(user)).Returns("delete-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("delete-token", result.DeletionToken);
        Assert.True(emailCode.IsConsumed);
        _emailCodeRepositoryMock.Verify(
            x => x.UpdateAsync(emailCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIssueDeletionToken_WhenPasswordlessUserHasValidEmailMfaCode()
    {
        var now = DateTime.UtcNow;
        var user = User.CreateFromExternalLogin(
            Email.Create("external@example.com"),
            Username.Create("externaluser"),
            "Google",
            "google-sub",
            now);
        user.EnableEmailMfa(["rec"], now);
        var emailCode = MfaEmailCode.Issue(user.Id, "h:123456", now.AddMinutes(10), now);
        var command = new CreateAccountDeletionChallengeCommand(user.Id, null, "123456", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _emailCodeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailCode);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");
        _jwtTokenServiceMock.Setup(x => x.GenerateAccountDeletionChallengeToken(user)).Returns("delete-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("delete-token", result.DeletionToken);
    }

    [Fact]
    public async Task Handle_ShouldRejectPasswordlessUser_WhenNoMfaIsEnabled()
    {
        var user = User.CreateFromExternalLogin(
            Email.Create("external@example.com"),
            Username.Create("externaluser"),
            "Google",
            "google-sub",
            DateTime.UtcNow);
        var command = new CreateAccountDeletionChallengeCommand(user.Id, null, null, null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccountDeletionChallengeToken(It.IsAny<User>()),
            Times.Never);
    }

    private static string AttemptIdentifier(Guid userId)
    {
        return $"account-deletion:{userId:N}";
    }
}
