using Legi.Contracts.Catalog;
using Legi.Contracts.Identity;
using Legi.Contracts.Library;
using Legi.Messaging.DependencyInjection;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Domain.Repositories;
using Legi.Social.Infrastructure.Persistence;
using Legi.Social.Infrastructure.Persistence.Repositories;
using Legi.SharedKernel;
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
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddDbContext<SocialDbContext>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("SocialDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }).AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

        services.AddLegiMessaging<SocialDbContext>("social", configuration);

        services.AddScoped<IUserDataPurger, UserDataPurger>();
        services.AddIntegrationEventConsumer<UserRegisteredIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<UserDeletedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<BookCreatedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<BookUpdatedIntegrationEvent, SocialDbContext>();

        // Library → Social feed projection (Phase 4C). Handlers are auto-registered
        // by the Application reflection scan; each consumer host owns one queue.
        services.AddIntegrationEventConsumer<BookAddedToLibraryIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<ReadingStatusChangedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<ReadingPostCreatedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<ReadingPostDeletedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<ReviewCreatedIntegrationEvent, SocialDbContext>();
        services.AddIntegrationEventConsumer<UserBookRatedIntegrationEvent, SocialDbContext>();

        // Write repositories (Domain interfaces)
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IContentSnapshotRepository, ContentSnapshotRepository>();
        services.AddScoped<IFeedItemRepository, FeedItemRepository>();
        services.AddScoped<IBookSnapshotRepository, BookSnapshotRepository>();

        // Read repositories (Application interfaces)
        services.AddScoped<IFollowReadRepository, FollowReadRepository>();
        services.AddScoped<ICommentReadRepository, CommentReadRepository>();
        services.AddScoped<ILikeReadRepository, LikeReadRepository>();
        services.AddScoped<IFeedItemReadRepository, FeedItemReadRepository>();
        services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();

        return services;
    }
}
