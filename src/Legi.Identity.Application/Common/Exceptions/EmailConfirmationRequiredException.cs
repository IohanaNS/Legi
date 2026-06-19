namespace Legi.Identity.Application.Common.Exceptions;

public class EmailConfirmationRequiredException(string message = "Email confirmation is required.")
    : Exception(message);
