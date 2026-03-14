using Legi.Library.Application.Common.DTOs;
using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetMyLibrary;

public record GetMyLibraryQuery(
    Guid UserId,
    ReadingStatus? StatusFilter = null,
    bool? WishlistFilter = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<UserBookDto>>;