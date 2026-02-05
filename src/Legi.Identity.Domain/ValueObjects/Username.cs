using System.Text.RegularExpressions;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.ValueObjects;

public sealed class Username : ValueObject
{
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Username is required");

        value = value.Trim().ToLowerInvariant();

        if (value.Length < 3)
            throw new DomainException("Username must be at least 3 characters");

        if (value.Length > 30)
            throw new DomainException("Username must be at most 30 characters");

        if (!IsValidFormat(value))
            throw new DomainException("Username must contain only letters, numbers and underscore");

        return new Username(value);
    }

    private static bool IsValidFormat(string username)
    {
        // Only letters, numbers and underscore
        // Must start with a letter
        var pattern = @"^[a-z][a-z0-9_]*$";
        return Regex.IsMatch(username, pattern);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Username username) => username.Value;
}
