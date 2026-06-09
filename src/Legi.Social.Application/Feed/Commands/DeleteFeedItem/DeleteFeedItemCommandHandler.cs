using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Feed.Commands.DeleteFeedItem;

public class DeleteFeedItemCommandHandler(IFeedItemRepository feedItemRepository)
    : IRequestHandler<DeleteFeedItemCommand>
{
    public async Task Handle(
        DeleteFeedItemCommand request,
        CancellationToken cancellationToken)
    {
        var feedItem = await feedItemRepository.GetByIdAsync(request.FeedItemId, cancellationToken);
        if (feedItem is null)
            throw new NotFoundException(nameof(FeedItem), request.FeedItemId);

        if (feedItem.ActorId != request.ActorId)
            throw new ForbiddenException("Only the activity owner can delete this feed item.");

        if (feedItem.TargetType is not null)
            throw new ConflictException(
                "This feed item is backed by content and must be deleted through its source.");

        await feedItemRepository.DeleteAsync(feedItem, cancellationToken);
    }
}
