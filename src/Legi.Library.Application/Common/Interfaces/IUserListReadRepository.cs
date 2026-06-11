using Legi.Library.Application.Common.DTOs;

namespace Legi.Library.Application.Common.Interfaces;

public interface IUserListReadRepository
{
    Task<IReadOnlyList<UserListSummaryDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<UserListSummaryDto>> GetVisibleByUserIdAsync(
        Guid targetUserId,
        Guid viewerUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountVisibleByUserIdAsync(
        Guid targetUserId,
        Guid viewerUserId,
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

    /// <summary>
    /// Returns summaries for the given list ids, restricted to public lists.
    /// Used to hydrate followed-list references (which carry only ids) from the
    /// Social context. Order is not guaranteed — callers that need the original
    /// ordering should reorder by the requested id sequence.
    /// </summary>
    Task<IReadOnlyList<UserListSummaryDto>> GetPublicSummariesByIdsAsync(
        IReadOnlyList<Guid> listIds,
        CancellationToken cancellationToken = default);
}
