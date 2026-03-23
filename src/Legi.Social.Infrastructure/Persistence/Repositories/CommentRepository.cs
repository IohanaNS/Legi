using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class CommentRepository(SocialDbContext context) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Comments
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await context.Comments.AddAsync(comment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        context.Comments.Remove(comment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
