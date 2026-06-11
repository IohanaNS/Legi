using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.UserLists.Queries.GetListSummariesByIds;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Queries;

public class GetListSummariesByIdsQueryHandlerTests
{
    private readonly Mock<IUserListReadRepository> _readRepository = new();
    private readonly GetListSummariesByIdsQueryHandler _handler;

    public GetListSummariesByIdsQueryHandlerTests()
    {
        _handler = new GetListSummariesByIdsQueryHandler(_readRepository.Object);
    }

    [Fact]
    public async Task Handle_PassesIdsThroughToReadRepository()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        IReadOnlyList<UserListSummaryDto> summaries =
        [
            new(ids[0], Guid.NewGuid(), "Sci-Fi", null, true, 3, 0, DateTime.UtcNow, [])
        ];

        _readRepository
            .Setup(r => r.GetPublicSummariesByIdsAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        var result = await _handler.Handle(
            new GetListSummariesByIdsQuery(ids),
            CancellationToken.None);

        Assert.Same(summaries, result);
        _readRepository.Verify(
            r => r.GetPublicSummariesByIdsAsync(ids, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
