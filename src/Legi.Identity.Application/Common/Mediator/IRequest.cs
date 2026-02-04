namespace Legi.Identity.Application.Common.Mediator;

/// <summary>
/// Marker interface to represent a request with a response.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<TResponse>
{
}

/// <summary>
/// Marker interface to represent a request with a void response.
/// </summary>
public interface IRequest : IRequest<Unit>
{
}
