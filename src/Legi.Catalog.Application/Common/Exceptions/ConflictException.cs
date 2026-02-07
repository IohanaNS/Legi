namespace Legi.Catalog.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}