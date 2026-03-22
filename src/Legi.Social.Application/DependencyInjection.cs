using System.Reflection;
using FluentValidation;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Social.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register custom mediator
        services.AddScoped<IMediator, Mediator>();

        // Register all request handlers (commands and queries)
        RegisterRequestHandlers(services, assembly);

        // Register all notification handlers (domain event handlers)
        RegisterNotificationHandlers(services, assembly);

        // Register pipeline behaviors in execution order (first = outermost)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static void RegisterRequestHandlers(IServiceCollection services, Assembly assembly)
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

    private static void RegisterNotificationHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Select(t => new
            {
                Implementation = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .ToList()
            })
            .Where(x => x.Interfaces.Any())
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            foreach (var interfaceType in handlerType.Interfaces)
            {
                // AddScoped allows multiple handlers per notification type
                services.AddScoped(interfaceType, handlerType.Implementation);
            }
        }
    }
}