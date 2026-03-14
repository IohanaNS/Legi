using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Queries.GetUserBookPosts;

public class GetUserBookPostsQueryHandler
    : IRequestHandler<GetUserBookPostsQuery, PaginatedList<ReadingPostDto>>
{
    private readonly IReadingPostReadRepository _readRepository;

    public GetUserBookPostsQueryHandler(IReadingPostReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PaginatedList<ReadingPostDto>> Handle(
        GetUserBookPostsQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.GetByUserBookIdAsync(
            request.UserBookId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
