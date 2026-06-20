using Legi.Identity.Application.Auth.Commands.GoogleSignIn;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class GoogleSignInCommandHandlerTests
{
    private const string Provider = "google";
    private const string Sub = "google-sub-123";
    private const string GoogleEmail = "newuser@gmail.com";

    private readonly Mock<IGoogleTokenValidator> _validatorMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtTokenService> _tokenServiceMock = new();
    private readonly GoogleSignInCommandHandler _handler;

    public GoogleSignInCommandHandlerTests()
    {
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("access_token", DateTime.UtcNow.AddHours(1)));
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
        _tokenServiceMock.Setup(x => x.HashRefreshToken("refresh_token")).Returns("refresh_token_hash");
        _tokenServiceMock.Setup(x => x.GetRefreshTokenExpiresAt()).Returns(DateTime.UtcNow.AddDays(7));

        _handler = new GoogleSignInCommandHandler(
            _validatorMock.Object,
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            Mock.Of<ILogger<GoogleSignInCommandHandler>>());
    }

    private void SetupValidToken(string email = GoogleEmail, bool emailVerified = true, string? name = "New User")
    {
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo(Sub, email, emailVerified, name, null));
    }

    private GoogleSignInCommand Command() => new("id-token", "127.0.0.1");

    [Fact]
    public async Task Handle_ShouldCreateNewUser_WhenNoMatchExists()
    {
        SetupValidToken();
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(GoogleEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? created = null;
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.NotNull(created);
        Assert.Null(created!.PasswordHash);
        Assert.True(created.IsEmailConfirmed);
        Assert.Equal(GoogleEmail, created.Email.Value);
        Assert.Single(created.ExternalLogins, l => l.Provider == Provider && l.ProviderKey == Sub);
        Assert.Equal("access_token", result.Token);
        Assert.Equal("refresh_token", result.RefreshToken);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnLinkedUser_WhenExternalLoginExists()
    {
        SetupValidToken();
        var existing = UserFactory.Create();
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(existing.Id, result.UserId);
        _userRepositoryMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldClaimUnconfirmedAccount_WhenLinkingByEmail()
    {
        // Pre-hijacking guard: an unconfirmed local account is claimed — its password
        // is revoked so a pre-registered attacker cannot keep access.
        var email = "existing@gmail.com";
        SetupValidToken(email: email);
        var existing = UserFactory.Create(email: Legi.Identity.Domain.ValueObjects.Email.Create(email), emailConfirmed: false);
        Assert.NotNull(existing.PasswordHash);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(existing.Id, result.UserId);
        Assert.Single(existing.ExternalLogins, l => l.Provider == Provider && l.ProviderKey == Sub);
        Assert.True(existing.IsEmailConfirmed); // linking confirms the email
        Assert.Null(existing.PasswordHash);     // untrusted credential revoked
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLinkConfirmedAccount_KeepingPassword()
    {
        var email = "confirmed@gmail.com";
        SetupValidToken(email: email);
        var existing = UserFactory.Create(email: Legi.Identity.Domain.ValueObjects.Email.Create(email), emailConfirmed: true);
        var passwordBefore = existing.PasswordHash;
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(existing.Id, result.UserId);
        Assert.Single(existing.ExternalLogins, l => l.Provider == Provider && l.ProviderKey == Sub);
        Assert.Equal(passwordBefore, existing.PasswordHash); // proven account keeps its credential
        _userRepositoryMock.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenTokenIsInvalid()
    {
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUserInfo?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(Command(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenEmailNotVerified()
    {
        SetupValidToken(emailVerified: false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(Command(), CancellationToken.None));
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSuffixUsername_WhenBaseIsTaken()
    {
        SetupValidToken(name: null); // seed from email local-part "newuser"
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(GoogleEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        // "newuser" is taken, suffixed variants are free.
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserFactory.Create());
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(It.Is<string>(u => u != "newuser"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? created = null;
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        await _handler.Handle(Command(), CancellationToken.None);

        Assert.NotNull(created);
        Assert.NotEqual("newuser", created!.Username.Value);
        Assert.StartsWith("newuser", created.Username.Value);
    }

    [Fact]
    public async Task Handle_ShouldRetryWithNewUsername_OnUsernameCollision()
    {
        // The conflict is neither the same sub nor the same email → username race → retry.
        SetupValidToken(name: null);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(GoogleEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.SetupSequence(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("username taken"))  // first attempt loses the race
            .Returns(Task.CompletedTask);                          // retry succeeds

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("access_token", result.Token);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldFallBackToExistingUser_OnCreateConflict()
    {
        SetupValidToken();
        var raceWinner = UserFactory.Create();

        // First lookup misses; after the create conflict, the re-query finds the winner.
        _userRepositoryMock.SetupSequence(x => x.GetByExternalLoginAsync(Provider, Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(raceWinner);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(GoogleEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("conflict"));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(raceWinner.Id, result.UserId);
        _userRepositoryMock.Verify(x => x.UpdateAsync(raceWinner, It.IsAny<CancellationToken>()), Times.Once);
    }
}
