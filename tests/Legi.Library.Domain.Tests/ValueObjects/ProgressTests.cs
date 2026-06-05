using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Tests.Factories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.ValueObjects;

public class ProgressTests
{
    [Fact]
    public void CreatePercentage_ValidValue_CreatesPercentageProgress()
    {
        var progress = Progress.CreatePercentage(75);

        Assert.Equal(75, progress.Value);
        Assert.Equal(ProgressType.Percentage, progress.Type);
        Assert.Equal("75%", progress.ToString());
    }

    [Fact]
    public void CreatePage_ValidValue_CreatesPageProgress()
    {
        var progress = Progress.CreatePage(120);

        Assert.Equal(120, progress.Value);
        Assert.Equal(ProgressType.Page, progress.Type);
        Assert.Equal("Page 120", progress.ToString());
    }

    [Theory]
    [InlineData(-1, ProgressType.Page)]
    [InlineData(-1, ProgressType.Percentage)]
    public void Create_NegativeValue_ThrowsDomainException(int value, ProgressType type)
    {
        Assert.Throws<DomainException>(() => Progress.Create(value, type));
    }

    [Fact]
    public void CreatePercentage_ValueAboveMaximum_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Progress.CreatePercentage(101));
    }

    [Fact]
    public void Completed_Always_CreatesOneHundredPercentProgress()
    {
        var progress = Progress.Completed();

        Assert.Equal(Progress.MaxPercentage, progress.Value);
        Assert.Equal(ProgressType.Percentage, progress.Type);
    }

    [Fact]
    public void Equals_SameValueAndType_ReturnsTrue()
    {
        var first = ProgressFactory.Percentage(50);
        var second = ProgressFactory.Percentage(50);

        Assert.Equal(first, second);
    }
}
