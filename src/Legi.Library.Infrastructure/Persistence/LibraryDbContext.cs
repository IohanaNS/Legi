using Legi.Library.Domain.Entities;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence;

public class LibraryDbContext : DbContext
{
    private readonly IMediator _mediator;

    public DbSet<UserBook> UserBooks => Set<UserBook>();
    public DbSet<ReadingProgress> ReadingPosts => Set<ReadingProgress>();
    public DbSet<UserList> UserLists => Set<UserList>();
    public DbSet<UserListItem> UserListItems => Set<UserListItem>();
    public DbSet<BookSnapshot> BookSnapshots => Set<BookSnapshot>();

    public LibraryDbContext(
        DbContextOptions<LibraryDbContext> options,
        IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Clear events from entities
        ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());

        // Save to database first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}