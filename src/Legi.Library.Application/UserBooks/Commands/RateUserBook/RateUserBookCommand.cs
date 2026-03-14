using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RateUserBook;

public record RateUserBookCommand(
    Guid UserBookId,
    Guid UserId,
    decimal Stars
) : IRequest<RateUserBookResponse>;