using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Queries.GetUserBookPosts;

public record GetUserBookPostsQuery(
    Guid UserBookId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<ReadingPostDto>>;
