namespace Legi.Catalog.Application.Common.Mediator;

/// <summary>
/// Represents an async continuation for the next task to execute in the pipeline.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Awaitable task returning the response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();