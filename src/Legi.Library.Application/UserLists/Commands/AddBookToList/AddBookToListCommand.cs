using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.AddBookToList;

public record AddBookToListCommand(
    Guid BookId,
    Guid ListId,
    Guid UserId
) : IRequest<AddBookToListResponse>;