using Legi.Identity.Application.Auth.Commands.ForgotPassword;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<IHumanVerificationService> _humanVerificationServiceMock = new();
    private readonly PasswordResetSettings _passwordResetSettings = new()
    {
        FrontendBaseUrl = "https://bukihub.test",
        TokenLifetimeMinutes = 60
    };
    private readonly TurnstileSettings _turnstileSettings = new() { Enabled = false, RequireForPasswordReset = true };

    private ForgotPasswordCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _emailSenderMock.Object,
        _passwordResetSettings,
        _turnstileSettings,
        _humanVerificationServiceMock.Object,
        NullLogger<ForgotPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldCreateTokenAndSendEmail_WhenUserExists()
    {
        // Arrange
        var user = UserFactory.CreateWithEmail("known@example.com");
        _userRepositoryMock
            .Setup(x => x.GetByEmailWithPasswordResetTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("known@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains(user.PasswordResetTokens, t => t.TokenHash == "token-hash" && t.IsActive);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(
            x => x.SendAsync(
                user.Email.Value,
                It.Is<EmailContent>(c =>
                    c.HtmlBody.Contains("https://bukihub.test/reset-password?token=raw-token") &&
                    c.TextBody.Contains("https://bukihub.test/reset-password?token=raw-token") &&
                    c.InlineImages != null && c.InlineImages.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailWithPasswordResetTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("unknown@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — anti-enumeration: identical behaviour, no token, no email, no throw
        _tokenFactoryMock.Verify(x => x.Create(), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTurnstileRequiredAndFails()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), HumanVerificationActions.PasswordReset, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("known@example.com");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HumanVerificationRequiredException>(act);
        _userRepositoryMock.Verify(
            x => x.GetByEmailWithPasswordResetTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
