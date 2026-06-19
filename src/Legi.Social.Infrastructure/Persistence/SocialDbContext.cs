using Legi.Messaging.DependencyInjection;
using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence;

public class SocialDbContext(DbContextOptions<SocialDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<ListFollow> ListFollows => Set<ListFollow>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ContentSnapshot> ContentSnapshots => Set<ContentSnapshot>();
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();
    public DbSet<BookSnapshot> BookSnapshots => Set<BookSnapshot>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyMessagingConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }
}
