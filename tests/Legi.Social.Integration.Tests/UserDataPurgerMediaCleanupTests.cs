using Legi.Social.Application.Common.Storage;
using Legi.Social.Domain.Entities;
using Legi.Social.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legi.Social.Integration.Tests;

public class UserDataPurgerMediaCleanupTests
{
    [SkippableFact]
    public async Task PurgeAsync_UserWithProfileMedia_DeletesStoredProfileMedia()
    {
        var conn = Environment.GetEnvironmentVariable("SOCIAL_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "SOCIAL_TEST_DB not set");

        var options = new DbContextOptionsBuilder<SocialDbContext>()
            .UseNpgsql(conn)
            .Options;

        var userId = Guid.NewGuid();
        var avatarUrl = $"/media/avatars/{userId}/avatar.webp";
        var bannerUrl = $"/media/banners/{userId}/banner.webp";

        await using var context = new SocialDbContext(options);
        var profile = UserProfile.Create(userId, "media" + userId.ToString("N")[..8]);
        profile.UpdateAvatar(avatarUrl);
        profile.UpdateBanner(bannerUrl);
        context.UserProfiles.Add(profile);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var storage = new RecordingObjectStorage();
        var purger = new UserDataPurger(
            context,
            storage,
            NullLogger<UserDataPurger>.Instance);

        await purger.PurgeAsync(userId);

        Assert.Contains(userId, storage.ProfileImageDeletes);
        Assert.Equal([avatarUrl, bannerUrl], storage.UrlDeletes);
    }

    private sealed class RecordingObjectStorage : IObjectStorage
    {
        public List<Guid> ProfileImageDeletes { get; } = new();
        public List<string> UrlDeletes { get; } = new();

        public Task<string> PutProfileImageAsync(
            Guid userId,
            ProfileImageKind kind,
            ProcessedImage image,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteByUrlAsync(string url, CancellationToken cancellationToken)
        {
            UrlDeletes.Add(url);
            return Task.CompletedTask;
        }

        public Task DeleteProfileImagesAsync(Guid userId, CancellationToken cancellationToken)
        {
            ProfileImageDeletes.Add(userId);
            return Task.CompletedTask;
        }
    }
}
