using System.Reflection;
using FluentValidation;
using Legi.Catalog.Application.Common.Mediator;
using Legi.Identity.Application.Common.Behaviors;

namespace Legi.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register custom mediator
        services.AddScoped<IMediator, Mediator>();

        // Register all handlers automatically
        RegisterHandlers(services, assembly);

        // Register pipeline behaviors in execution order (first = outermost)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Select(t => new
            {
                Implementation = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
                    .ToList()
            })
            .Where(x => x.Interfaces.Any())
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            foreach (var interfaceType in handlerType.Interfaces)
            {
                services.AddScoped(interfaceType, handlerType.Implementation);
            }
        }
    }
}