using Legi.Catalog.Application.Books.IntegrationEventHandlers;
using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.IntegrationEventHandlers;

public class UserDeletedIntegrationEventHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock = new();
    private readonly UserDeletedIntegrationEventHandler _handler;

    public UserDeletedIntegrationEventHandlerTests()
    {
        _handler = new UserDeletedIntegrationEventHandler(
            _bookRepositoryMock.Object,
            NullLogger<UserDeletedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldAnonymizeBooksCreatedByDeletedUser()
    {
        // Arrange
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var integrationEvent = new UserDeletedIntegrationEvent(userId, DateTime.UtcNow);

        _bookRepositoryMock
            .Setup(x => x.AnonymizeCreatorsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        await _handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        _bookRepositoryMock.Verify(
            x => x.AnonymizeCreatorsAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
