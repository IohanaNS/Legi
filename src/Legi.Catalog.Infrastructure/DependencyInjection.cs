using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.ExternalServices;
using Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;
using Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Legi.Contracts.Identity;
using Legi.Contracts.Library;
using Legi.Messaging.DependencyInjection;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("CatalogDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }).AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

        services.AddLegiMessaging<CatalogDbContext>("catalog", configuration);
        services.AddIntegrationEventConsumer<UserDeletedIntegrationEvent, CatalogDbContext>();

        // Library → Catalog rating recompute (Phase 5). UserBookRated is a second,
        // independent queue on the existing fanout exchange (Social consumes it too).
        services.AddIntegrationEventConsumer<UserBookRatedIntegrationEvent, CatalogDbContext>();
        services.AddIntegrationEventConsumer<UserBookRatingRemovedIntegrationEvent, CatalogDbContext>();

        // Library → Catalog reviews count (created/deleted). Independent queues on
        // the ReviewCreated / ReadingPostDeleted fanout exchanges.
        services.AddIntegrationEventConsumer<ReviewCreatedIntegrationEvent, CatalogDbContext>();
        services.AddIntegrationEventConsumer<ReadingPostDeletedIntegrationEvent, CatalogDbContext>();

        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBookRatingRepository, BookRatingRepository>();
        services.AddScoped<Reconciliation.BookRatingReconciler>();
        services.AddScoped<IBookReadRepository, BookReadRepository>();
        services.AddScoped<ITagReadRepository, TagReadRepository>();
        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();
        services.AddScoped<IExternalBookSearchQueue, ExternalBookSearchQueue>();
        services.AddScoped<IBookSearchAliasWriter, BookSearchAliasWriter>();
        services.AddSingleton<IBookCoverUrlResolver, OpenLibraryCoverUrlResolver>();
        services.AddHostedService<ExternalBookSearchWorker>();

        var openLibrarySettings = new OpenLibrarySettings();
        configuration.GetSection(OpenLibrarySettings.SectionName).Bind(openLibrarySettings);

        var googleBooksSettings = new GoogleBooksSettings();
        configuration.GetSection(GoogleBooksSettings.SectionName).Bind(googleBooksSettings);

        services.Configure<GoogleBooksSettings>(
            configuration.GetSection(GoogleBooksSettings.SectionName));
        services.Configure<OpenLibrarySettings>(
            configuration.GetSection(OpenLibrarySettings.SectionName));

        if (openLibrarySettings.Enabled)
        {
            services.AddHttpClient<OpenLibraryClient>(client =>
            {
                client.BaseAddress = new Uri("https://openlibrary.org");
                client.Timeout = TimeSpan.FromSeconds(openLibrarySettings.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", "Legi/1.0 (book-catalog-app)");
            });

            services.AddScoped<IExternalBookClient>(sp => sp.GetRequiredService<OpenLibraryClient>());
        }

        if (googleBooksSettings.Enabled)
        {
            services.AddHttpClient<GoogleBooksClient>(client =>
            {
                client.BaseAddress = new Uri("https://www.googleapis.com");
                client.Timeout = TimeSpan.FromSeconds(googleBooksSettings.TimeoutSeconds);
            });

            services.AddScoped<IExternalBookClient>(sp => sp.GetRequiredService<GoogleBooksClient>());
        }

        services.AddScoped<IBookDataProvider, BookDataProvider>();

        return services;
    }
}
