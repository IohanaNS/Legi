using Legi.Identity.Application.Auth.Commands.ResendConfirmation;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.ResendConfirmation;

public class ResendConfirmationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<IHumanVerificationService> _humanVerificationServiceMock = new();
    private readonly EmailConfirmationSettings _emailConfirmationSettings = new()
    {
        FrontendBaseUrl = "https://bukihub.test",
        TokenLifetimeMinutes = 1440
    };
    private readonly TurnstileSettings _turnstileSettings = new()
    {
        Enabled = false,
        RequireForEmailConfirmation = true
    };

    private ResendConfirmationCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _emailSenderMock.Object,
        _emailConfirmationSettings,
        _turnstileSettings,
        _humanVerificationServiceMock.Object,
        NullLogger<ResendConfirmationCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand("unknown@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenFactoryMock.Verify(x => x.Create(), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenUserIsAlreadyConfirmed()
    {
        // Arrange
        var user = UserFactory.Create();
        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                user.Email.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand(user.Email.Value);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenFactoryMock.Verify(x => x.Create(), Times.Never);
        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateTokenAndSendEmail_WhenUserIsUnconfirmed()
    {
        // Arrange
        var user = UserFactory.Create(emailConfirmed: false);
        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                user.Email.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand(user.Email.Value, Language: "en");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var token = Assert.Single(user.EmailConfirmationTokens);
        Assert.Equal("token-hash", token.TokenHash);
        Assert.True(token.IsActive);
        Assert.NotNull(token.SentAt);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _emailSenderMock.Verify(
            x => x.SendAsync(
                user.Email.Value,
                It.Is<EmailContent>(c =>
                    c.Subject == "Confirm your BukiHub email" &&
                    c.HtmlBody.Contains("https://bukihub.test/confirm-email?token=raw-token") &&
                    c.TextBody.Contains("https://bukihub.test/confirm-email?token=raw-token")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateTokenOrSendEmail_WhenWithinCooldown()
    {
        // Arrange
        var user = UserFactory.Create(emailConfirmed: false);
        user.AddEmailConfirmationToken("recent-token-hash", DateTime.UtcNow.AddHours(1));
        user.MarkEmailConfirmationTokenSent("recent-token-hash", DateTime.UtcNow);

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                user.Email.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand(user.Email.Value);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenFactoryMock.Verify(x => x.Create(), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateTokenAndSendEmail_WhenRecentTokenWasNotSent()
    {
        // Arrange
        var user = UserFactory.Create(emailConfirmed: false);
        var unsentToken = user.AddEmailConfirmationToken("unsent-token-hash", DateTime.UtcNow.AddHours(1));

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                user.Email.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "new-token-hash"));

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand(user.Email.Value);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(unsentToken.IsUsed);
        Assert.Contains(user.EmailConfirmationTokens, t =>
            t.TokenHash == "new-token-hash" &&
            t.IsActive &&
            t.SentAt.HasValue);
        _emailSenderMock.Verify(
            x => x.SendAsync(user.Email.Value, It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidatePriorActiveTokenAndSendEmail_WhenCooldownHasElapsed()
    {
        // Arrange
        var user = UserFactory.Create(emailConfirmed: false);
        var oldToken = user.AddEmailConfirmationToken("old-token-hash", DateTime.UtcNow.AddHours(1));
        user.MarkEmailConfirmationTokenSent("old-token-hash", DateTime.UtcNow.AddMinutes(-4));

        _userRepositoryMock
            .Setup(x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
                user.Email.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "new-token-hash"));

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand(user.Email.Value);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(oldToken.IsUsed);
        Assert.Contains(user.EmailConfirmationTokens, t =>
            t.TokenHash == "new-token-hash" &&
            t.IsActive &&
            t.SentAt.HasValue);
        _emailSenderMock.Verify(
            x => x.SendAsync(user.Email.Value, It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTurnstileRequiredAndFails()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync(null, null, HumanVerificationActions.EmailConfirmation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new ResendConfirmationCommand("known@example.com");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HumanVerificationRequiredException>(act);
        _userRepositoryMock.Verify(
            x => x.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

}
