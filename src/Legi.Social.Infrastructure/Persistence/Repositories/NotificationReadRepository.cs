using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class NotificationReadRepository(SocialDbContext context) : INotificationReadRepository
{
    public async Task<PaginatedList<NotificationDto>> GetNotificationsAsync(
        Guid recipientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == recipientId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                ActorId = n.ActorId,
                ActorUsername = n.ActorUsername,
                ActorAvatarUrl = n.ActorAvatarUrl,
                NotificationType = n.NotificationType.ToString(),
                TargetType = n.TargetType.ToString(),
                TargetId = n.TargetId,
                CommentPreview = n.CommentPreview,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<NotificationDto>(items, totalCount, page, pageSize);
    }

    public Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientId == recipientId && !n.IsRead, cancellationToken);
    }
}
