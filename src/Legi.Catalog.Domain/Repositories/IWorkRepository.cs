using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Write repository for the <see cref="Work"/> aggregate. Works are resolved by
/// their <see cref="Legi.Catalog.Domain.ValueObjects.WorkKey"/> during import
/// (resolve-or-create) so editions sharing a key land under the same work.
/// </summary>
public interface IWorkRepository
{
    Task<Work?> GetByWorkKeyAsync(string workKey, CancellationToken cancellationToken = default);
    Task AddAsync(Work work, CancellationToken cancellationToken = default);
    Task UpdateAsync(Work work, CancellationToken cancellationToken = default);
}
