using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence;

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<UserBook> UserBooks => Set<UserBook>();
    public DbSet<ReadingProgress> ReadingPosts => Set<ReadingProgress>();
    public DbSet<UserList> UserLists => Set<UserList>();
    public DbSet<UserListItem> UserListItems => Set<UserListItem>();
    public DbSet<BookSnapshot> BookSnapshots => Set<BookSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}