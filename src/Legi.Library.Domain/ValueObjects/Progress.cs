using Legi.Library.Domain.Enums;
using Legi.SharedKernel;

namespace Legi.Library.Domain.ValueObjects;

public sealed class Progress : ValueObject
{
    public int Value { get; set; }
    public ProgressType Type { get; set; }
    public static int MaxPercentage { get; set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        throw new NotImplementedException();
    }

    public static Progress? Completed()
    {
        throw new NotImplementedException();
    }
}