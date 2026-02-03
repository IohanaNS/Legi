using FluentAssertions;
using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldLoginWithValidCredentials()
    {
        // Arrange
        var command = new LoginCommand("teste@exemplo.com", "SenhaCorreta123!");
        var user = CreateValidUser();

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
            .Returns("refresh_token_hash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email.Value);
        result.Token.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token_hash");

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
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
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials");

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
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
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials");

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldAddRefreshTokenToUser()
    {
        // Arrange
        var command = LoginCommandFactory.Create();
        var user = CreateValidUser();
        var tokenCountBefore = user.RefreshTokens.Count;

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

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.RefreshTokens.Should().HaveCount(tokenCountBefore + 1);
        user.RefreshTokens.Should().Contain(t => t.TokenHash == "new_refresh_token");
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

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // Helper method
    private User CreateValidUser() => UserFactory.Create();
}
