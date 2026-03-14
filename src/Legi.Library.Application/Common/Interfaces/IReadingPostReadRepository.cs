using Legi.Library.Application.Common.DTOs;

namespace Legi.Library.Application.Common.Interfaces;

public interface IReadingPostReadRepository
{
    Task<PaginatedList<ReadingPostDto>> GetByUserBookIdAsync(
        Guid userBookId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}