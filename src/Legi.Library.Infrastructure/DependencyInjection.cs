using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Domain.Repositories;
using Legi.Library.Infrastructure.Persistence;
using Legi.Library.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("LibraryDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(LibraryDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));

        // Write repositories (Domain interfaces)
        services.AddScoped<IUserBookRepository, UserBookRepository>();
        services.AddScoped<IReadingPostRepository, ReadingPostRepository>();
        services.AddScoped<IUserListRepository, UserListRepository>();
        services.AddScoped<IBookSnapshotRepository, BookSnapshotRepository>();

        // Read repositories (Application interfaces)
        services.AddScoped<IUserBookReadRepository, UserBookReadRepository>();
        services.AddScoped<IReadingPostReadRepository, ReadingPostReadRepository>();
        services.AddScoped<IUserListReadRepository, UserListReadRepository>();

        return services;
    }
}