using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILoginAttemptRepository> _loginAttemptRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly Mock<IHumanVerificationService> _humanVerificationServiceMock;
    private readonly LoginLockoutSettings _loginLockoutSettings;
    private readonly TurnstileSettings _turnstileSettings;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loginAttemptRepositoryMock = new Mock<ILoginAttemptRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<IJwtTokenService>();
        _humanVerificationServiceMock = new Mock<IHumanVerificationService>();
        _loginLockoutSettings = new LoginLockoutSettings
        {
            MaxFailedAttempts = 3,
            FailureWindowMinutes = 15,
            LockoutDurationMinutes = 15
        };
        _turnstileSettings = new TurnstileSettings
        {
            Enabled = false,
            LoginFailedAttemptsBeforeRequired = 2
        };

        _loginAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginAttempt?)null);

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _loginAttemptRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _loginLockoutSettings,
            _turnstileSettings,
            _humanVerificationServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldLoginWithValidCredentials()
    {
        // Arrange
        var command = new LoginCommand("teste@exemplo.com", "SenhaCorreta123!");
        var user = CreateValidUser();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(14);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("access_token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(refreshTokenExpiresAt);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email.Value, result.Email);
        Assert.Equal("access_token", result.Token);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal(refreshTokenExpiresAt, result.RefreshTokenExpiresAt);

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _loginAttemptRepositoryMock.Verify(
            x => x.ClearAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedExceptionWhenUserDoesNotExist()
    {
        // Arrange
        var command = LoginCommandFactory.CreateWithEmail("inexistente@exemplo.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid credentials", exception.Message);

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
        _loginAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                "inexistente@exemplo.com",
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedExceptionWhenPasswordIsIncorrect()
    {
        // Arrange
        var command = LoginCommandFactory.Create(password: "SenhaErrada!");
        var user = CreateValidUser();

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid credentials", exception.Message);

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never
        );

        _loginAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                command.EmailOrUsername,
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRecordFailedAttemptWhenPasswordIsIncorrect()
    {
        // Arrange
        var command = LoginCommandFactory.Create(password: "SenhaErrada!");
        var user = CreateValidUser();

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid credentials", exception.Message);

        _loginAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                command.EmailOrUsername,
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowEmailConfirmationRequired_WhenPasswordIsCorrectAndEmailIsUnconfirmed()
    {
        // Arrange
        var command = LoginCommandFactory.Create();
        var user = UserFactory.Create(emailConfirmed: false);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<EmailConfirmationRequiredException>(act);

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LastFailedLoginAt);
        Assert.Null(user.LoginLockoutEndsAt);
        Assert.Empty(user.RefreshTokens);
        _loginAttemptRepositoryMock.Verify(
            x => x.ClearAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrectAndEmailIsUnconfirmed()
    {
        // Arrange
        var command = LoginCommandFactory.Create(password: "SenhaErrada!");
        var user = UserFactory.Create(emailConfirmed: false);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid credentials", exception.Message);
        _loginAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                command.EmailOrUsername,
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRejectLockedAccountWithoutCheckingPassword()
    {
        // Arrange
        var command = LoginCommandFactory.Create(password: "SenhaCorreta123!");
        var now = DateTime.UtcNow;
        var loginAttempt = LoginAttempt.Create(command.EmailOrUsername, now.AddMinutes(-1));

        for (var i = 0; i < _loginLockoutSettings.MaxFailedAttempts; i++)
        {
            loginAttempt.RecordFailedLogin(
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                now.AddSeconds(i));
        }

        _loginAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginAttempt);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid credentials", exception.Message);

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
        _userRepositoryMock.Verify(
            x => x.GetByEmailOrUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRequireTurnstileWhenAccountHasFailedAttempts()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        var command = LoginCommandFactory.Create(turnstileToken: null);
        var now = DateTime.UtcNow;
        var loginAttempt = LoginAttempt.Create(command.EmailOrUsername, now.AddMinutes(-1));

        for (var i = 0; i < _turnstileSettings.LoginFailedAttemptsBeforeRequired; i++)
        {
            loginAttempt.RecordFailedLogin(
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                now.AddSeconds(i));
        }

        _loginAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginAttempt);

        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync(null, null, HumanVerificationActions.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HumanVerificationRequiredException>(act);

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
        _userRepositoryMock.Verify(
            x => x.GetByEmailOrUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRequireTurnstileWhenUnknownIdentifierHasFailedAttempts()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        var command = LoginCommandFactory.CreateWithEmail("unknown@example.com");
        var now = DateTime.UtcNow;
        var loginAttempt = LoginAttempt.Create(command.EmailOrUsername, now.AddMinutes(-1));

        for (var i = 0; i < _turnstileSettings.LoginFailedAttemptsBeforeRequired; i++)
        {
            loginAttempt.RecordFailedLogin(
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                now.AddSeconds(i));
        }

        _loginAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginAttempt);

        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync(null, null, HumanVerificationActions.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HumanVerificationRequiredException>(act);
        _userRepositoryMock.Verify(
            x => x.GetByEmailOrUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _loginAttemptRepositoryMock.Verify(
            x => x.RecordFailedAttemptAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldCheckPasswordWhenTurnstilePasses()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        var command = LoginCommandFactory.Create(turnstileToken: "turnstile-token");
        var user = CreateValidUser();
        var now = DateTime.UtcNow;
        var loginAttempt = LoginAttempt.Create(command.EmailOrUsername, now.AddMinutes(-1));

        for (var i = 0; i < _turnstileSettings.LoginFailedAttemptsBeforeRequired; i++)
        {
            loginAttempt.RecordFailedLogin(
                _loginLockoutSettings.MaxFailedAttempts,
                _loginLockoutSettings.FailureWindow,
                _loginLockoutSettings.LockoutDuration,
                now.AddSeconds(i));
        }

        _loginAttemptRepositoryMock
            .Setup(x => x.GetByIdentifierAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginAttempt);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync("turnstile-token", null, HumanVerificationActions.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
        _passwordHasherMock.Verify(
            x => x.Verify(command.Password, user.PasswordHash),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldAddRefreshTokenToUser()
    {
        // Arrange
        var command = LoginCommandFactory.Create();
        var user = CreateValidUser();
        var tokenCountBefore = user.RefreshTokens.Count;
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(14);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("new_refresh_token"))
            .Returns("new_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(refreshTokenExpiresAt);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("new_refresh_token", result.RefreshToken);
        Assert.Equal(tokenCountBefore + 1, user.RefreshTokens.Count);
        Assert.Contains(user.RefreshTokens, t => t.TokenHash == "new_refresh_token_hash");
        Assert.Contains(user.RefreshTokens, t => t.ExpiresAt == refreshTokenExpiresAt);
        Assert.DoesNotContain(user.RefreshTokens, t => t.TokenHash == "new_refresh_token");
    }

    [Fact]
    public async Task Handle_ShouldPersistUserAfterLogin()
    {
        // Arrange
        var command = LoginCommandFactory.Create();
        var user = CreateValidUser();

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh"))
            .Returns("refresh_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(DateTime.UtcNow.AddDays(14));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldClearFailedLoginAttemptsAfterSuccessfulLogin()
    {
        // Arrange
        var command = LoginCommandFactory.Create();
        var user = CreateValidUser();
        user.RecordFailedLogin(
            _loginLockoutSettings.MaxFailedAttempts,
            _loginLockoutSettings.FailureWindow,
            _loginLockoutSettings.LockoutDuration,
            DateTime.UtcNow.AddMinutes(-1));

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh"))
            .Returns("refresh_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(DateTime.UtcNow.AddDays(14));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LastFailedLoginAt);
        Assert.Null(user.LoginLockoutEndsAt);
        _loginAttemptRepositoryMock.Verify(
            x => x.ClearAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // Helper method
    private User CreateValidUser() => UserFactory.Create();
}
