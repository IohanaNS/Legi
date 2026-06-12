using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.ValueObjects;

/// <summary>
/// Groups editions that are the same abstract work. Two creation paths, in
/// authoritative-signal order (see Docs/CATALOG-FEATURE-editions.md, "Merge
/// policy — LOCKED: bias to under-merge"):
///
/// 1. <see cref="FromProvider"/> — an external provider's curated work key
///    (OpenLibrary's <c>/works/OL…W</c>). Stored as <c>ol:OL…W</c>.
/// 2. <see cref="Synthesize"/> — a deterministic <c>syn:{title}|{author}</c> key
///    derived from normalized title + primary author, used only when no provider
///    key is known (Google-sourced books, OL gaps).
///
/// The two namespaces (<c>ol:</c> / <c>syn:</c>) never collide. We deliberately
/// keep subtitles and always include the author so unrelated books don't collapse
/// (over-merge corrupts; under-merge is recoverable via a later merge).
/// </summary>
public sealed partial class WorkKey : ValueObject
{
    /// <summary>Cap each synthesized segment so the stored key stays bounded.</summary>
    private const int MaxSegmentLength = 120;

    public string Value { get; }

    private WorkKey(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Builds a key from a provider work identifier, e.g. OpenLibrary's
    /// <c>/works/OL45804W</c> (or a bare <c>OL45804W</c>) → <c>ol:OL45804W</c>.
    /// </summary>
    public static WorkKey FromProvider(string providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new DomainException("Provider work key is required");

        // OpenLibrary keys look like "/works/OL45804W"; take the last segment and
        // preserve its case (OL ids are case-sensitive).
        var id = providerKey
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault() ?? providerKey.Trim();

        return new WorkKey($"ol:{id}");
    }

    /// <summary>
    /// Builds a deterministic synthesized key from normalized title + primary
    /// author. Last resort when no provider key is available.
    /// </summary>
    public static WorkKey Synthesize(string title, string? primaryAuthor)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required to synthesize a work key");

        var titleSlug = Slug(title);
        var authorSlug = Slug(NormalizeAuthor(primaryAuthor));

        return new WorkKey($"syn:{titleSlug}|{authorSlug}");
    }

    /// <summary>
    /// Resolves a work key using the locked precedence: a provider key when
    /// present, otherwise a synthesized title+author key.
    /// </summary>
    public static WorkKey Resolve(string? providerKey, string title, string? primaryAuthor)
    {
        return !string.IsNullOrWhiteSpace(providerKey)
            ? FromProvider(providerKey)
            : Synthesize(title, primaryAuthor);
    }

    public bool IsSynthesized => Value.StartsWith("syn:", StringComparison.Ordinal);

    /// <summary>
    /// "Last, First" → "First Last" so author-name ordering doesn't fragment a
    /// work. Other formats pass through untouched.
    /// </summary>
    private static string NormalizeAuthor(string? author)
    {
        if (string.IsNullOrWhiteSpace(author))
            return string.Empty;

        var trimmed = author.Trim();
        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex < 0)
            return trimmed;

        var last = trimmed[..commaIndex].Trim();
        var first = trimmed[(commaIndex + 1)..].Trim();
        return $"{first} {last}".Trim();
    }

    /// <summary>
    /// Lowercases, strips diacritics and punctuation, and collapses everything
    /// else into a single hyphen-separated slug. Subtitles are kept (a colon just
    /// becomes a hyphen) so series volumes don't over-merge.
    /// </summary>
    private static string Slug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip diacritics: decompose then drop the combining marks.
        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var stripped = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var slug = NonSlugCharsRegex().Replace(stripped, "-").Trim('-');

        return slug.Length > MaxSegmentLength ? slug[..MaxSegmentLength].Trim('-') : slug;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(WorkKey workKey) => workKey.Value;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharsRegex();
}
