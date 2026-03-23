using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class CommentReadRepository(SocialDbContext context) : ICommentReadRepository
{
    public async Task<PaginatedList<CommentDto>> GetByTargetAsync(
        InteractableType targetType,
        Guid targetId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Comments
            .AsNoTracking()
            .Where(c => c.TargetType == targetType && c.TargetId == targetId)
            .Join(
                context.UserProfiles,
                c => c.UserId,
                up => up.UserId,
                (c, up) => new { Comment = c, Profile = up });

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Comment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CommentDto
            {
                Id = x.Comment.Id,
                UserId = x.Comment.UserId,
                Username = x.Profile.Username,
                AvatarUrl = x.Profile.AvatarUrl,
                Content = x.Comment.Content,
                CreatedAt = x.Comment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<CommentDto>(items, totalCount, page, pageSize);
    }
}
