using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.ExternalServices;
using Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;
using Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
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

        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookReadRepository, BookReadRepository>();
        services.AddScoped<ITagReadRepository, TagReadRepository>();
        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();
        
        services.AddHttpClient<OpenLibraryClient>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Legi/1.0 (book-catalog-app)");
        });

        services.AddHttpClient<GoogleBooksClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

// Register individual clients
        services.AddScoped<IExternalBookClient, OpenLibraryClient>();
        services.AddScoped<IExternalBookClient, GoogleBooksClient>();

// Register orchestrator
        services.AddScoped<IBookDataProvider, BookDataProvider>();

        return services;
    }
}