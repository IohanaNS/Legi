using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Feed.Commands.DeleteFeedItem;

public record DeleteFeedItemCommand(Guid ActorId, Guid FeedItemId) : IRequest;
