using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Domain.Repositories;
using Legi.Social.Infrastructure.Persistence;
using Legi.Social.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Social.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SocialDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("SocialDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));

        // Write repositories (Domain interfaces)
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IContentSnapshotRepository, ContentSnapshotRepository>();
        services.AddScoped<IFeedItemRepository, FeedItemRepository>();

        // Read repositories (Application interfaces)
        services.AddScoped<IFollowReadRepository, FollowReadRepository>();
        services.AddScoped<ICommentReadRepository, CommentReadRepository>();
        services.AddScoped<ILikeReadRepository, LikeReadRepository>();
        services.AddScoped<IFeedItemReadRepository, FeedItemReadRepository>();

        return services;
    }
}
