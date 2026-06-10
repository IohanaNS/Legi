using Legi.Library.Application.Common.DTOs;
using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetUserLibrary;

/// <summary>
/// A public, paginated view of another user's library (e.g. the books they have
/// read). <paramref name="ViewerUserId"/> is captured for future visibility/block
/// rules; it is unused today (any authenticated user may view).
/// </summary>
public record GetUserLibraryQuery(
    Guid TargetUserId,
    Guid? ViewerUserId = null,
    ReadingStatus? StatusFilter = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<UserBookDto>>;
