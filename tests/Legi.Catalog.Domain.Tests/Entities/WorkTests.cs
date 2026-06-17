using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.Entities;

public class WorkTests
{
    private static WorkKey Key() => WorkKey.Synthesize("Dune", "Frank Herbert");

    [Fact]
    public void Create_TrimsTitleAndCover_AndInitializes()
    {
        var work = Work.Create(Key(), "  Dune  ", "  https://covers/dune.jpg  ");

        Assert.Equal("Dune", work.Title);
        Assert.Equal("https://covers/dune.jpg", work.DefaultCoverUrl);
        Assert.Equal("syn:dune|frank-herbert", work.WorkKey.Value);
        Assert.NotEqual(Guid.Empty, work.Id);
    }

    [Fact]
    public void Create_NormalizesBlankCoverToNull()
    {
        var work = Work.Create(Key(), "Dune", "   ");

        Assert.Null(work.DefaultCoverUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenTitleIsEmpty(string title)
    {
        Assert.Throws<DomainException>(() => Work.Create(Key(), title));
    }

    [Fact]
    public void EnsureDefaultCover_SetsCover_WhenNoneYet()
    {
        var work = Work.Create(Key(), "Dune");

        work.EnsureDefaultCover("https://covers/dune.jpg");

        Assert.Equal("https://covers/dune.jpg", work.DefaultCoverUrl);
    }

    [Fact]
    public void EnsureDefaultCover_DoesNotClobberExistingCover()
    {
        var work = Work.Create(Key(), "Dune", "https://covers/first.jpg");

        work.EnsureDefaultCover("https://covers/second.jpg");

        Assert.Equal("https://covers/first.jpg", work.DefaultCoverUrl);
    }

    [Fact]
    public void EnsureDefaultCover_IgnoresBlankInput()
    {
        var work = Work.Create(Key(), "Dune");

        work.EnsureDefaultCover("   ");

        Assert.Null(work.DefaultCoverUrl);
    }
}
