using Legi.Messaging.DependencyInjection;
using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence;

public class SocialDbContext(DbContextOptions<SocialDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ContentSnapshot> ContentSnapshots => Set<ContentSnapshot>();
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();
    public DbSet<BookSnapshot> BookSnapshots => Set<BookSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyMessagingConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }
}
