using Legi.Messaging.Inbox;
using Legi.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Legi.Messaging.DependencyInjection;

/// <summary>
/// EF Core <see cref="ModelBuilder"/> extensions for registering the messaging
/// entity configurations. Call from each service's
/// <c>DbContext.OnModelCreating</c>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the configurations for <see cref="OutboxMessage"/> and
    /// <see cref="InboxMessage"/>. Without this call, the messaging tables
    /// will not be created by EF Core migrations.
    /// </summary>
    public static ModelBuilder ApplyMessagingConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        return modelBuilder;
    }
}