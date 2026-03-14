using Legi.Library.Application.Common.DTOs;

namespace Legi.Library.Application.Common.Interfaces;

public interface IUserListReadRepository
{
    Task<IReadOnlyList<UserListSummaryDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserListDetailDto?> GetDetailByIdAsync(
        Guid listId,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<UserListBookDto>> GetListBooksAsync(
        Guid listId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<UserListSummaryDto>> SearchPublicAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}