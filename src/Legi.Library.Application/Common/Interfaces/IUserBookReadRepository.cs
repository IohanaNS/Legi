using Legi.Library.Application.Common.DTOs;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.Common.Interfaces;

public interface IUserBookReadRepository
{
    Task<PaginatedList<UserBookDto>> GetByUserIdAsync(
        Guid userId,
        ReadingStatus? statusFilter,
        bool? wishlistFilter,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}