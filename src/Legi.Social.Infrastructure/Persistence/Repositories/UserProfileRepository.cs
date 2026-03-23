using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class UserProfileRepository(SocialDbContext context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await context.UserProfiles.AddAsync(profile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        context.UserProfiles.Update(profile);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        context.UserProfiles.Remove(profile);
        await context.SaveChangesAsync(cancellationToken);
    }
}
