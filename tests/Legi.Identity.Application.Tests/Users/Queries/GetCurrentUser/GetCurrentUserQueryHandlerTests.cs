using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Queries.GetCurrentUser;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetCurrentUserQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentUserResponse_WhenUserExists()
    {
        // Arrange
        var query = GetCurrentUserQueryFactory.Create();
        var user = UserFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email.Value, result.Email);
        Assert.Equal(user.Username.Value, result.Username);
        Assert.Equal(0, result.Stats.TotalBooks);
        Assert.Equal(0, result.Stats.TotalFollowers);
    }

    [Fact]
    public async Task Handle_ShouldThrowApplicationException_WhenUserDoesNotExist()
    {
        // Arrange
        var query = GetCurrentUserQueryFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ApplicationException>(act);
        Assert.Equal("USER_NOT_FOUND", exception.Message);
    }
}
