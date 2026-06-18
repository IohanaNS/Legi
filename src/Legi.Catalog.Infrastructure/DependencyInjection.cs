using Amazon.S3;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.ExternalServices;
using Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;
using Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Legi.Catalog.Infrastructure.Storage;
using Legi.Contracts.Identity;
using Legi.Contracts.Library;
using Legi.Messaging.DependencyInjection;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        // Owned cover storage: validate-by-fetching across providers, then upload
        // real covers to the separate legi-covers bucket on the same MinIO as
        // Social. Once owned, a cover is immune to the providers disappearing.
        services.Configure<CatalogStorageOptions>(configuration.GetSection(CatalogStorageOptions.SectionName));
        services.Configure<CoverSourceOptions>(configuration.GetSection(CoverSourceOptions.SectionName));
        services.AddKeyedSingleton<IAmazonS3>(S3BookCoverStorage.S3ClientKey, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<CatalogStorageOptions>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = true // required for MinIO and most non-AWS gateways
            };
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });
        services.AddHttpClient(HttpBookCoverSource.HttpClientName, client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Legi/1.0 (book-catalog-app)");
        });
        services.AddSingleton<IBookCoverStorage, S3BookCoverStorage>();
        services.AddSingleton<IBookCoverSource, HttpBookCoverSource>();
        services.AddSingleton<IBookCoverAcquisition, BookCoverAcquisition>();
        services.AddSingleton<IBookCoverImageProcessor, ImageSharpCoverProcessor>();

        // Durable discovery for books imported cover-less: bounded decaying retry
        // → Exhausted, distinguishing provider outages from confirmed no-cover.
        services.AddScoped<ICoverIngestionQueue, CoverIngestionQueue>();
        services.AddHostedService<CoverIngestionWorker>();

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
