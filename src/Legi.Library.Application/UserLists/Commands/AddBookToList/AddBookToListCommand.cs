using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.AddBookToList;

public record AddBookToListCommand(
    Guid UserBookId,
    Guid ListId,
    Guid UserId
) : IRequest<AddBookToListResponse>;