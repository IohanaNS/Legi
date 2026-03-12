using System.Reflection;
using FluentValidation;
using Legi.Library.Application.Common.Behaviors;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediator(assembly, cfg =>
        {
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
        return services;
    }
    
}