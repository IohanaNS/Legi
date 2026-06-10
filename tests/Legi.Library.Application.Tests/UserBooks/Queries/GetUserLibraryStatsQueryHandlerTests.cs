using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Queries.GetUserLibraryStats;
using Legi.Library.Domain.Enums;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Queries;

public class GetUserLibraryStatsQueryHandlerTests
{
    private readonly Mock<IUserBookReadRepository> _userBookReadRepository = new();
    private readonly Mock<IUserListReadRepository> _userListReadRepository = new();
    private readonly GetUserLibraryStatsQueryHandler _handler;

    public GetUserLibraryStatsQueryHandlerTests()
    {
        _handler = new GetUserLibraryStatsQueryHandler(
            _userBookReadRepository.Object,
            _userListReadRepository.Object);
    }

    [Fact]
    public async Task Handle_StatusCountsAndVisibleLists_ReturnsStats()
    {
        var counts = new Dictionary<ReadingStatus, int>
        {
            [ReadingStatus.Reading] = 2,
            [ReadingStatus.Finished] = 3,
            [ReadingStatus.NotStarted] = 1
        };

        _userBookReadRepository
            .Setup(r => r.GetStatusCountsByUserIdAsync(
                LibraryTestIds.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);
        _userListReadRepository
            .Setup(r => r.CountVisibleByUserIdAsync(
                LibraryTestIds.UserId,
                LibraryTestIds.OtherUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(
            new GetUserLibraryStatsQuery(LibraryTestIds.UserId, LibraryTestIds.OtherUserId),
            CancellationToken.None);

        Assert.Equal(2, result.Reading);
        Assert.Equal(3, result.Finished);
        Assert.Equal(0, result.Paused);
        Assert.Equal(0, result.Abandoned);
        Assert.Equal(1, result.NotStarted);
        Assert.Equal(4, result.Lists);
    }

    [Fact]
    public async Task Handle_NoBooks_ReturnsZeroStatusCounts()
    {
        _userBookReadRepository
            .Setup(r => r.GetStatusCountsByUserIdAsync(
                LibraryTestIds.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ReadingStatus, int>());
        _userListReadRepository
            .Setup(r => r.CountVisibleByUserIdAsync(
                LibraryTestIds.UserId,
                LibraryTestIds.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(
            new GetUserLibraryStatsQuery(LibraryTestIds.UserId, LibraryTestIds.UserId),
            CancellationToken.None);

        Assert.Equal(0, result.Reading);
        Assert.Equal(0, result.Finished);
        Assert.Equal(0, result.Paused);
        Assert.Equal(0, result.Abandoned);
        Assert.Equal(0, result.NotStarted);
        Assert.Equal(0, result.Lists);
    }
}
