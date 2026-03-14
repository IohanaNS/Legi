using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListBooks;

public record GetListBooksQuery(
    Guid ListId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<UserListBookDto>>;
