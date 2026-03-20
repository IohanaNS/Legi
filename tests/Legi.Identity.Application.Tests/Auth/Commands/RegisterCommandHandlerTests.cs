using Legi.Identity.Application.Auth.Commands.Register;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldRegisterNewUserSuccessfully()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(
            email: "novo@exemplo.com",
            username: "novousr"
        );

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.Hash(command.Password))
            .Returns("hashed_password");

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("access_token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token_hash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Email.ToLowerInvariant(), result.Email);
        Assert.Equal(command.Username.ToLowerInvariant(), result.Username);
        Assert.Equal("access_token", result.Token);
        Assert.Equal("refresh_token_hash", result.RefreshToken);

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
    }

    [Fact]
    public async Task Handle_ShouldHashThePassword()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(password: "SenhaSuperSecreta123!");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.Hash(command.Password))
            .Returns("hashed_password");

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(
            x => x.Hash(command.Password),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldGenerateTokensForNewUser()
    {
        // Arrange
        var command = RegisterCommandFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hash");

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("access_token", DateTime.UtcNow.AddHours(1)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
    }
}
