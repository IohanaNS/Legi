namespace Legi.Identity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    // Registers application-layer services. Add concrete registrations here.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Example:
        // services.AddScoped<IUserService, UserService>();
        return services;
    }
}