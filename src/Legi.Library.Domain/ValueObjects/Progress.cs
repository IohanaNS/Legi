using Legi.Library.Domain.Enums;
using Legi.SharedKernel;

namespace Legi.Library.Domain.ValueObjects;

public sealed class Progress : ValueObject
{
    public const int MaxPercentage = 100;
    public int Value { get; set; }
    public ProgressType Type { get; set; }

    private Progress(int value, ProgressType type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Creates a "Progress" with the given value and type.
    /// Validates: value >= 0, and percentage <= 100.
    /// Page upper bound (PageCount) is validated externally.
    /// </summary>
    public static Progress Create(int value, ProgressType type)
    {
        if (value < 0)
            throw new DomainException("Progress value cannot be negative");

        if (type == ProgressType.Percentage && value > MaxPercentage)
            throw new DomainException(
                $"Percentage progress cannot exceed {MaxPercentage}");

        return new Progress(value, type);
    }

    public static Progress CreatePercentage(int value)
        => Create(value, ProgressType.Percentage);

    public static Progress CreatePage(int value)
        => Create(value, ProgressType.Page);

    /// <summary>
    /// Convenience factory for 100% completion.
    /// Used by UserBook when status changes to Finished.
    /// </summary>
    public static Progress Completed()
        => CreatePercentage(MaxPercentage);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }

    public override string ToString() => Type == ProgressType.Percentage
        ? $"{Value}%"
        : $"Page {Value}";
}