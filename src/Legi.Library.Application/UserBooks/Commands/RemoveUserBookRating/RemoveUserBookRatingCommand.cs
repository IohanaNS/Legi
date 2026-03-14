using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveUserBookRating;

public record RemoveUserBookRatingCommand(
    Guid UserBookId,
    Guid UserId
) : IRequest<Unit>;