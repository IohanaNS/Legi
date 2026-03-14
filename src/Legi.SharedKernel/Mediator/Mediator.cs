namespace Legi.SharedKernel.Mediator;

/// <summary>
/// Default mediator implementation that relays requests to registered handlers through a pipeline of behaviors.
/// </summary>
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        // Build the handler interface type: IRequestHandler<TRequest, TResponse>
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        // Resolve the handler from DI
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        // Resolve all pipeline behaviors for this request/response type
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = (serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(behaviorType))
            as IEnumerable<object> ?? []).Reverse().ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> pipeline = () =>
        {
            // This is the innermost delegate - it calls the actual handler
            var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))
                ?? throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

            var result = handleMethod.Invoke(handler, [request, cancellationToken]);

            if (result is Task<TResponse> task)
                return task;

            throw new InvalidOperationException($"Handler did not return Task<{typeof(TResponse).Name}>");
        };

        // Wrap the handler with behaviors (in reverse order so first registered = outermost)
        foreach (var behavior in behaviors)
        {
            var currentPipeline = pipeline;
            var currentBehavior = behavior;

            pipeline = () =>
            {
                var handleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.Handle))
                    ?? throw new InvalidOperationException($"Handle method not found on {behaviorType.Name}");

                var result = handleMethod.Invoke(currentBehavior, [request, currentPipeline, cancellationToken]);

                if (result is Task<TResponse> task)
                    return task;

                throw new InvalidOperationException($"Behavior did not return Task<{typeof(TResponse).Name}>");
            };
        }

        // Execute the pipeline
        return await pipeline();
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        // Try to find IRequestHandler<TRequest> first (void handler)
        var voidHandlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var handler = serviceProvider.GetService(voidHandlerType);

        if (handler != null)
        {
            // This is a void request - call the handler directly without Unit wrapping
            var handleMethod = voidHandlerType.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Handle method not found on {voidHandlerType.Name}");

            var result = handleMethod.Invoke(handler, [request, cancellationToken]);

            if (result is not Task task) throw new InvalidOperationException($"Handler did not return Task");
            await task;
            return;
        }

        // Fall back to IRequestHandler<TRequest, Unit>
        await Send(request as IRequest<Unit> ?? throw new InvalidOperationException($"Request does not implement IRequest<Unit>"), cancellationToken);
    }

    public async Task Publish(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = notification.GetType();

        // Build INotificationHandler<TNotification>
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

        // Resolve ALL handlers (zero or many)
        var handlersEnumerable = serviceProvider.GetService(
            typeof(IEnumerable<>).MakeGenericType(handlerType));

        if (handlersEnumerable is not IEnumerable<object> handlers)
            return;

        // Execute each handler sequentially
        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.Handle))
                ?? throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

            var result = handleMethod.Invoke(handler, [notification, cancellationToken]);

            if (result is Task task)
                await task;
        }
    }
}