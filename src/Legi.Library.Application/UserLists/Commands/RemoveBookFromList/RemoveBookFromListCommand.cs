using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.RemoveBookFromList;

public record RemoveBookFromListCommand(
    Guid BookId,
    Guid ListId,
    Guid UserId
) : IRequest<Unit>;