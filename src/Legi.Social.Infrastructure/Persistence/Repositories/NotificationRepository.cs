using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class NotificationRepository(SocialDbContext context) : INotificationRepository
{
    public void StageAdd(Notification notification)
    {
        // Id is client-generated (ValueGeneratedNever), so a synchronous Add is
        // safe. No SaveChanges — the like/comment SaveChanges commits this row too.
        context.Notifications.Add(notification);
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        DateTime? now = DateTime.UtcNow;
        return context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now),
                cancellationToken);
    }

    public Task BulkUpdateActorUsernameAsync(
        Guid actorId, string newUsername, CancellationToken cancellationToken = default)
    {
        return context.Notifications
            .Where(n => n.ActorId == actorId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.ActorUsername, newUsername),
                cancellationToken);
    }
}
