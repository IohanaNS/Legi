using Legi.Identity.Application.Common.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling {RequestName} with payload {@Request}",
            typeof(TRequest).Name,
            request);

        var response = await next();

        _logger.LogInformation(
            "Handled {RequestName}",
            typeof(TRequest).Name);

        return response;
    }
}