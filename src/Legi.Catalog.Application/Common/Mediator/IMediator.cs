namespace Legi.Catalog.Application.Common.Mediator;

/// <summary>
/// Defines a mediator to encapsulate request/response and publishing interaction patterns.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Asynchronously send a request to a single handler.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response.</returns>
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously send a request with a void response.
    /// </summary>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    Task Send(IRequest request, CancellationToken cancellationToken = default);
}