namespace Legi.Social.Application.Common.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string entity, object key)
        : this($"{entity} with key '{key}' was not found.") { }
}