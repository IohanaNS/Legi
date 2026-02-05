using System.Text.RegularExpressions;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required");

        value = value.Trim().ToLowerInvariant();

        if (value.Length > 255)
            throw new DomainException("Email must be at most 255 characters");

        if (!IsValidFormat(value))
            throw new DomainException("Invalid email format");

        return new Email(value);
    }

    private static bool IsValidFormat(string email)
    {
        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}