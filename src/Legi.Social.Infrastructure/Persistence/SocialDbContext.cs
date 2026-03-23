using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence;

public class SocialDbContext : DbContext
{
    private readonly IMediator _mediator;

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ContentSnapshot> ContentSnapshots => Set<ContentSnapshot>();
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();

    public SocialDbContext(
        DbContextOptions<SocialDbContext> options,
        IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving (Follow, Like, Comment inherit BaseEntity)
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
