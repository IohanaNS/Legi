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

    /// <summary>
    /// The viewer's active (non-deleted) UserBook for a given book, or null when
    /// the book is not in their library. Drives the book details page header.
    /// </summary>
    Task<UserBookDto?> GetByUserAndBookAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default);
}