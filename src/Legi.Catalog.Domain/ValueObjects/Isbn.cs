using Legi.SharedKernel;

namespace Legi.Catalog.Domain.ValueObjects;

public sealed class Isbn : ValueObject
{
    public string Value { get; }

    private Isbn(string value)
    {
        Value = value;
    }

    public static Isbn Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("ISBN is required");

        // Remove hyphens and spaces
        var cleaned = value.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        switch (cleaned.Length)
        {
            case 10:
            {
                if (!IsValidIsbn10(cleaned))
                    throw new DomainException("Invalid ISBN-10 checksum");
                break;
            }
            case 13:
            {
                if (!IsValidIsbn13(cleaned))
                    throw new DomainException("Invalid ISBN-13 checksum");
                break;
            }
            default:
                throw new DomainException("ISBN must be 10 or 13 characters");
        }

        return new Isbn(cleaned);
    }

    /// <summary>
    /// Validates ISBN-10 checksum.
    /// Formula: (10*d1 + 9*d2 + 8*d3 + ... + 1*d10) mod 11 == 0
    /// Last digit can be 'X' representing 10.
    /// </summary>
    private static bool IsValidIsbn10(string isbn)
    {
        if (isbn.Length != 10)
            return false;

        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digit;
            if (i == 9 && isbn[i] == 'X')
            {
                digit = 10;
            }
            else if (char.IsDigit(isbn[i]))
            {
                digit = isbn[i] - '0';
            }
            else
            {
                return false;
            }

            sum += (10 - i) * digit;
        }

        return sum % 11 == 0;
    }

    /// <summary>
    /// Validates ISBN-13 checksum.
    /// Formula: (d1 + 3*d2 + d3 + 3*d4 + ... + d13) mod 10 == 0
    /// </summary>
    private static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Length != 13)
            return false;

        if (!isbn.All(char.IsDigit))
            return false;

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            var digit = isbn[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        return sum % 10 == 0;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Isbn isbn) => isbn.Value;
}
