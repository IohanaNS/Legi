namespace Legi.Library.Application.UserLists.Commands.AddBookToList;

public record AddBookToListResponse(
    Guid ListId,
    Guid BookId,
    int BooksCount
);