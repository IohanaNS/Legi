using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Queries.GetPublicProfile;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Queries.GetPublicProfile;

public class GetPublicProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetPublicProfileQueryHandler _handler;

    public GetPublicProfileQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetPublicProfileQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfileWithNullFollowState_WhenRequestIsAnonymous()
    {
        // Arrange
        var query = GetPublicProfileQueryFactory.Create(currentUserId: null);
        var user = UserFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Username.Value, result.Username);
        Assert.Null(result.IsFollowedByMe);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfileWithFalseFollowState_WhenRequestIsAuthenticated()
    {
        // Arrange
        var query = GetPublicProfileQueryFactory.CreateAuthenticated();
        var user = UserFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsFollowedByMe!.Value);
        Assert.Equal(0, result.Stats.TotalReviews);
    }

    [Fact]
    public async Task Handle_ShouldThrowApplicationException_WhenUserDoesNotExist()
    {
        // Arrange
        var query = GetPublicProfileQueryFactory.Create();

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
