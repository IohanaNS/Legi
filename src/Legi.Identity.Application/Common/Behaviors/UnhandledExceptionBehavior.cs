using MediatR;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Common.Behaviors;

public sealed class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception for request {RequestName} {@Request}",
                typeof(TRequest).Name,
                request);

            throw;
        }
    }
}