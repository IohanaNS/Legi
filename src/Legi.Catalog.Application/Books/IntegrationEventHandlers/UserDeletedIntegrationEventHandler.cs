using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Identity;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Catalog's consumer for <see cref="UserDeletedIntegrationEvent"/>. Anonymizes
/// the <c>CreatedByUserId</c> field on every book the deleted user added to the
/// catalog, so the books themselves persist (other users may have them in their
/// libraries) but with no specific creator attribution.
///
/// Uses <see cref="IBookRepository.AnonymizeCreatorsAsync"/> — a bulk SQL update
/// via <c>ExecuteUpdateAsync</c>. This bypasses the change tracker and commits
/// independently of the dispatcher's inbox-row save. Idempotent: re-running on
/// a delivery duplicate updates zero rows on the second pass.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 8.1 (bulk operations
/// exception) and section 6.2 (UserDeleted cascade table).
///
/// MUST NOT call SaveChangesAsync — see decision 8.1.
/// </summary>
public sealed class UserDeletedIntegrationEventHandler(
    IBookRepository bookRepository,
    ILogger<UserDeletedIntegrationEventHandler> logger)
    : INotificationHandler<UserDeletedIntegrationEvent>
{
    public async Task Handle(
        UserDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var affected = await bookRepository.AnonymizeCreatorsAsync(
            integrationEvent.UserId,
            cancellationToken);

        logger.LogInformation(
            "Anonymized {Count} book(s) created by deleted user {UserId}",
            affected, integrationEvent.UserId);
    }
}
