namespace Legi.Catalog.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public IReadOnlyDictionary<string, object?> Extensions { get; }

    public ConflictException(string message) : base(message)
    {
        Extensions = new Dictionary<string, object?>();
    }

    public ConflictException(string message, IReadOnlyDictionary<string, object?> extensions) : base(message)
    {
        Extensions = extensions;
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Extensions = new Dictionary<string, object?>();
    }
}
