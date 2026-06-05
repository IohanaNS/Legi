using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Application.Profiles.Queries.SearchUsers;
using Moq;

namespace Legi.Social.Application.Tests.Profiles.Queries.SearchUsers;

public class SearchUsersQueryHandlerTests
{
    private readonly Mock<IUserProfileReadRepository> _userProfileReadRepository = new();
    private readonly SearchUsersQueryHandler _handler;

    public SearchUsersQueryHandlerTests()
    {
        _handler = new SearchUsersQueryHandler(_userProfileReadRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsRepositoryResultsAndNormalizesPrefix()
    {
        var viewerUserId = Guid.NewGuid();
        var results = new List<FollowUserDto>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                Username = "alice",
                AvatarUrl = "https://cdn.example.com/alice.png",
                Bio = "Reads classics.",
                IsFollowedByViewer = true
            }
        };

        string? capturedPrefix = null;
        Guid? capturedViewerUserId = null;
        int capturedLimit = 0;

        _userProfileReadRepository
            .Setup(r => r.SearchByUsernamePrefixAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, int, CancellationToken>((prefix, viewerId, limit, _) =>
            {
                capturedPrefix = prefix;
                capturedViewerUserId = viewerId;
                capturedLimit = limit;
            })
            .ReturnsAsync(results);

        var query = new SearchUsersQuery(" Ali ", viewerUserId, 7);

        var response = await _handler.Handle(query, CancellationToken.None);

        Assert.Same(results, response);
        Assert.Equal("ali", capturedPrefix);
        Assert.Equal(viewerUserId, capturedViewerUserId);
        Assert.Equal(7, capturedLimit);
    }
}
