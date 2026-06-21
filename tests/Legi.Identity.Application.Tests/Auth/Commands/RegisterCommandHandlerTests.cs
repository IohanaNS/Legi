using FluentValidation;
using Legi.Identity.Application.Auth.Commands.Register;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IHumanVerificationService> _humanVerificationServiceMock;
    private readonly Mock<IBreachedPasswordChecker> _breachedPasswordCheckerMock;
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock;
    private readonly EmailConfirmationSettings _emailConfirmationSettings;
    private readonly TurnstileSettings _turnstileSettings;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenFactoryMock = new Mock<ISecureTokenFactory>();
        _emailSenderMock = new Mock<IEmailSender>();
        _humanVerificationServiceMock = new Mock<IHumanVerificationService>();
        _breachedPasswordCheckerMock = new Mock<IBreachedPasswordChecker>();
        _breachedPasswordCheckerMock
            .Setup(x => x.IsBreachedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _auditLoggerMock = new Mock<ISecurityAuditLogger>();
        _emailConfirmationSettings = new EmailConfirmationSettings
        {
            FrontendBaseUrl = "https://bukihub.test",
            TokenLifetimeMinutes = 1440
        };
        _turnstileSettings = new TurnstileSettings
        {
            Enabled = false,
            RequireForRegistration = true
        };

        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenFactoryMock.Object,
            _emailSenderMock.Object,
            _emailConfirmationSettings,
            _turnstileSettings,
            _humanVerificationServiceMock.Object,
            _breachedPasswordCheckerMock.Object,
            _auditLoggerMock.Object,
            NullLogger<RegisterCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_ShouldRegisterNewUserSuccessfully()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(
            email: "novo@exemplo.com",
            username: "novousr",
            language: "pt-BR"
        );

        SetupNoExistingUser(command);
        _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed_password");
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));

        User? addedUser = null;
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Email.ToLowerInvariant(), result.Email);
        Assert.Equal(command.Username.ToLowerInvariant(), result.Username);
        Assert.True(result.EmailConfirmationRequired);
        Assert.NotNull(addedUser);
        Assert.False(addedUser.IsEmailConfirmed);
        Assert.Empty(addedUser.RefreshTokens);
        var confirmationToken = Assert.Single(addedUser.EmailConfirmationTokens);
        Assert.Equal("token-hash", confirmationToken.TokenHash);
        Assert.True(confirmationToken.IsActive);
        Assert.NotNull(confirmationToken.SentAt);

        _emailSenderMock.Verify(
            x => x.SendAsync(
                addedUser.Email.Value,
                It.Is<EmailContent>(c =>
                    c.Subject == "Confirme seu e-mail do BukiHub" &&
                    c.HtmlBody.Contains("https://bukihub.test/confirm-email?token=raw-token") &&
                    c.TextBody.Contains("https://bukihub.test/confirm-email?token=raw-token") &&
                    c.InlineImages != null && c.InlineImages.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(addedUser, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictExceptionWhenEmailAlreadyExists()
    {
        // Arrange
        var command = RegisterCommandFactory.CreateWithEmail("existente@exemplo.com");
        var existingUser = UserFactory.CreateWithEmail("existente@exemplo.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(act);
        Assert.Equal("A user with this email already exists.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _emailSenderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRequireTurnstileWhenEnabled()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        var command = RegisterCommandFactory.Create(turnstileToken: null);

        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync(null, null, HumanVerificationActions.Register, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HumanVerificationRequiredException>(act);

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldContinueWhenTurnstilePasses()
    {
        // Arrange
        _turnstileSettings.Enabled = true;
        var command = RegisterCommandFactory.Create(turnstileToken: "turnstile-token");

        _humanVerificationServiceMock
            .Setup(x => x.VerifyAsync("turnstile-token", null, HumanVerificationActions.Register, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupNoExistingUser(command);
        _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed_password");
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldHashThePassword()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(password: "SenhaSuperSecreta123!");

        SetupNoExistingUser(command);
        _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed_password");
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(
            x => x.Hash(command.Password),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateAccount_WhenConfirmationEmailSendFails()
    {
        // Arrange
        var command = RegisterCommandFactory.Create();

        SetupNoExistingUser(command);
        _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed_password");
        _tokenFactoryMock.Setup(x => x.Create()).Returns(("raw-token", "token-hash"));
        _emailSenderMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        User? addedUser = null;
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.EmailConfirmationRequired);
        Assert.NotNull(addedUser);
        Assert.Null(addedUser.EmailConfirmationTokens.Single().SentAt);
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRejectBreachedPassword()
    {
        // Arrange
        var command = RegisterCommandFactory.Create();
        SetupNoExistingUser(command);
        _breachedPasswordCheckerMock
            .Setup(x => x.IsBreachedAsync(command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(act);
        Assert.Contains(exception.Errors, e => e.PropertyName == "Password");
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupNoExistingUser(RegisterCommand command)
    {
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(command.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
    }
}
