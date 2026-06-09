using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetMyUserBookByBook;

/// <summary>
/// Returns the viewer's active UserBook for a book, or null if not in library.
/// Drives the book details page (status pill, rating, progress, userBookId).
/// </summary>
public record GetMyUserBookByBookQuery(
    Guid UserId,
    Guid BookId
) : IRequest<UserBookDto?>;
