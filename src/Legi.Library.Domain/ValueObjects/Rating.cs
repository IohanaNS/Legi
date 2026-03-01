using Legi.SharedKernel;

namespace Legi.Library.Domain.ValueObjects;

public sealed class Rating : ValueObject
{
    public const int MinValue = 1;
    public const int MaxValue = 10;

    /// <summary>
    /// Internal value representing half-stars (1-10).
    /// 1 = 0.5 stars, 2 = 1.0 stars, ..., 10 = 5.0 stars.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Star representation for display (0.5 - 5.0).
    /// </summary>
    public decimal Stars => Value / 2.0m;

    private Rating(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Rating from the internal half-star value (1-10).
    /// </summary>
    public static Rating Create(int value)
    {
        if (value < MinValue || value > MaxValue)
            throw new DomainException(
                $"Rating must be between {MinValue} and {MaxValue} (half-stars)");

        return new Rating(value);
    }

    /// <summary>
    /// Creates a Rating from a star value (0.5 - 5.0).
    /// Input must be in increments of 0.5.
    /// </summary>
    public static Rating FromStars(decimal stars)
    {
        if (stars % 0.5m != 0)
            throw new DomainException("Rating must be in increments of 0.5 stars");

        var value = (int)(stars * 2);
        return Create(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Stars:F1} stars";
}