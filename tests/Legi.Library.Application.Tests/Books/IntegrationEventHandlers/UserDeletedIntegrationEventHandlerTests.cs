using Legi.Library.Application.Books.IntegrationEventHandlers;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.Books.IntegrationEventHandlers;

public class UserDeletedIntegrationEventHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IReadingPostRepository> _readingPostRepository = new();
    private readonly Mock<IUserListRepository> _userListRepository = new();
    private readonly UserDeletedIntegrationEventHandler _handler;

    public UserDeletedIntegrationEventHandlerTests()
    {
        _handler = new UserDeletedIntegrationEventHandler(
            _userBookRepository.Object,
            _readingPostRepository.Object,
            _userListRepository.Object,
            NullLogger<UserDeletedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_UserDeletedEvent_DeletesAllLibraryDataForUser()
    {
        var integrationEvent = IdentityIntegrationEventFactory.UserDeleted();
        _readingPostRepository
            .Setup(r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _userListRepository
            .Setup(r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _userBookRepository
            .Setup(r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        await _handler.Handle(integrationEvent, CancellationToken.None);

        _readingPostRepository.Verify(
            r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _userListRepository.Verify(
            r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _userBookRepository.Verify(
            r => r.DeleteAllForUserAsync(integrationEvent.UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
