using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class WorkRepository(CatalogDbContext context) : IWorkRepository
{
    public Task<Work?> GetByWorkKeyAsync(string workKey, CancellationToken cancellationToken = default)
    {
        return context.Works
            .FirstOrDefaultAsync(w => w.WorkKey.Value == workKey, cancellationToken);
    }

    public Task<Work?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Works.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task AddAsync(Work work, CancellationToken cancellationToken = default)
    {
        await context.Works.AddAsync(work, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Work work, CancellationToken cancellationToken = default)
    {
        context.Works.Update(work);
        await context.SaveChangesAsync(cancellationToken);
    }
}
