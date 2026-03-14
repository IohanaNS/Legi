namespace Legi.Library.Application.UserBooks.Commands.RateUserBook;

public record RateUserBookResponse(
    Guid UserBookId,
    decimal Stars,
    DateTime UpdatedAt
);