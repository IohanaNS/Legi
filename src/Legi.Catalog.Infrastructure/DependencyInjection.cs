using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("CatalogDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));

        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ITagReadRepository, TagReadRepository>();
        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();

        return services;
    }
}