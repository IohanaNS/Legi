using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.UpdateUserBook;

public record UpdateUserBookCommand(
    Guid UserBookId,
    Guid UserId,
    ReadingStatus? Status = null,
    bool? Wishlist = null,
    int? ProgressValue = null,
    ProgressType? ProgressType = null
) : IRequest<UpdateUserBookResponse>;