using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.ValueObjects;

public class WorkKeyTests
{
    [Theory]
    [InlineData("/works/OL45804W", "ol:OL45804W")]
    [InlineData("OL45804W", "ol:OL45804W")]
    [InlineData("  /works/OL45804W  ", "ol:OL45804W")]
    public void FromProvider_ExtractsAndNamespacesTheOpenLibraryId(string providerKey, string expected)
    {
        Assert.Equal(expected, WorkKey.FromProvider(providerKey).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromProvider_Throws_WhenProviderKeyIsEmpty(string providerKey)
    {
        Assert.Throws<DomainException>(() => WorkKey.FromProvider(providerKey));
    }

    [Fact]
    public void Synthesize_ProducesNamespacedTitleAuthorSlug()
    {
        var key = WorkKey.Synthesize("Dune", "Frank Herbert");

        Assert.Equal("syn:dune|frank-herbert", key.Value);
        Assert.True(key.IsSynthesized);
    }

    [Theory]
    // Case, punctuation and whitespace differences normalize to the same key.
    [InlineData("Dune", "Frank Herbert")]
    [InlineData("  dune  ", "frank herbert")]
    [InlineData("DUNE!", "Frank   Herbert")]
    public void Synthesize_IsStableAcrossCasingAndPunctuation(string title, string author)
    {
        Assert.Equal("syn:dune|frank-herbert", WorkKey.Synthesize(title, author).Value);
    }

    [Fact]
    public void Synthesize_StripsDiacritics()
    {
        Assert.Equal(
            "syn:l-ete|gabriel-garcia-marquez",
            WorkKey.Synthesize("L'été", "Gabriel García Márquez").Value);
    }

    [Fact]
    public void Synthesize_KeepsSubtitles_SoSeriesVolumesDoNotOverMerge()
    {
        var volumeOne = WorkKey.Synthesize("The Lord of the Rings: The Fellowship of the Ring", "Tolkien");
        var volumeTwo = WorkKey.Synthesize("The Lord of the Rings: The Two Towers", "Tolkien");

        Assert.NotEqual(volumeOne.Value, volumeTwo.Value);
    }

    [Fact]
    public void Synthesize_NormalizesLastFirstAuthorOrdering()
    {
        var lastFirst = WorkKey.Synthesize("Dune", "Herbert, Frank");
        var firstLast = WorkKey.Synthesize("Dune", "Frank Herbert");

        Assert.Equal(firstLast.Value, lastFirst.Value);
    }

    [Fact]
    public void Synthesize_DoesNotCollapseDifferentBooksWithSameTitle()
    {
        // Under-merge bias: same title, different author must stay distinct.
        var a = WorkKey.Synthesize("Collected Poems", "W. B. Yeats");
        var b = WorkKey.Synthesize("Collected Poems", "Philip Larkin");

        Assert.NotEqual(a.Value, b.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Synthesize_Throws_WhenTitleIsEmpty(string title)
    {
        Assert.Throws<DomainException>(() => WorkKey.Synthesize(title, "Frank Herbert"));
    }

    [Fact]
    public void Resolve_PrefersProviderKeyOverSynthesis()
    {
        var key = WorkKey.Resolve("/works/OL45804W", "Dune", "Frank Herbert");

        Assert.Equal("ol:OL45804W", key.Value);
        Assert.False(key.IsSynthesized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_SynthesizesWhenNoProviderKey(string? providerKey)
    {
        var key = WorkKey.Resolve(providerKey, "Dune", "Frank Herbert");

        Assert.Equal("syn:dune|frank-herbert", key.Value);
    }

    [Fact]
    public void WorkKeys_WithSameValue_AreEqual()
    {
        Assert.Equal(WorkKey.Synthesize("Dune", "Frank Herbert"), WorkKey.Synthesize("Dune", "Frank Herbert"));
        Assert.Equal(WorkKey.FromProvider("/works/OL1W"), WorkKey.FromProvider("OL1W"));
    }
}
