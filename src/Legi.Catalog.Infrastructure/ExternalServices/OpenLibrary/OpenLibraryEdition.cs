using System.Text.Json;
using System.Text.Json.Serialization;

namespace Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Maps to: GET https://openlibrary.org/isbn/{isbn}.json
/// This is the Edition endpoint — returns book-specific (not work-level) data.
/// 
/// Open Library has two levels:
/// - Edition = a specific publication (has ISBN, publisher, page count)
/// - Work = the abstract "book" concept (has description, subjects, all editions)
/// We query the Edition first, then optionally fetch the Work for description.
/// </summary>
internal record OpenLibraryEdition
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("authors")]
    public List<OpenLibraryAuthorRef>? Authors { get; init; }

    [JsonPropertyName("number_of_pages")]
    public int? NumberOfPages { get; init; }

    [JsonPropertyName("publishers")]
    public List<string>? Publishers { get; init; }

    /// <summary>
    /// Internal Open Library cover IDs. Used to construct cover URLs:
    /// https://covers.openlibrary.org/b/id/{coverId}-L.jpg
    /// </summary>
    [JsonPropertyName("covers")]
    public List<long>? Covers { get; init; }

    [JsonPropertyName("languages")]
    public List<OpenLibraryRef>? Languages { get; init; }

    /// <summary>
    /// Description can be either a plain string OR an object { "type": "...", "value": "..." }.
    /// The custom converter handles both cases.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonConverter(typeof(OpenLibraryDescriptionConverter))]
    public string? Description { get; init; }

    /// <summary>
    /// References to the Work(s) this edition belongs to.
    /// Format: [{ "key": "/works/OL45804W" }]
    /// Used to fetch work-level description when edition has none.
    /// </summary>
    [JsonPropertyName("works")]
    public List<OpenLibraryRef>? Works { get; init; }
}

/// <summary>
/// Author reference in an edition. Contains only a key (not the actual name).
/// Must be resolved via: GET https://openlibrary.org{key}.json
/// </summary>
internal record OpenLibraryAuthorRef
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }  // e.g. "/authors/OL34184A"
}

/// <summary>
/// Generic Open Library reference with a key field.
/// Used for languages ("/languages/eng"), works ("/works/OL45804W"), etc.
/// </summary>
internal record OpenLibraryRef
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}

/// <summary>
/// Maps to: GET https://openlibrary.org/authors/{authorId}.json
/// We only need the name field.
/// </summary>
internal record OpenLibraryAuthor
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

/// <summary>
/// Maps to: GET https://openlibrary.org/works/{workId}.json
/// Used as fallback to get description when the edition doesn't have one.
/// </summary>
internal record OpenLibraryWork
{
    [JsonPropertyName("description")]
    [JsonConverter(typeof(OpenLibraryDescriptionConverter))]
    public string? Description { get; init; }
}

/// <summary>
/// Handles Open Library's inconsistent description format.
/// Sometimes it's a plain string: "A dystopian novel..."
/// Sometimes it's an object: { "type": "/type/text", "value": "A dystopian novel..." }
/// </summary>
internal class OpenLibraryDescriptionConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? value = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "value")
                {
                    reader.Read();
                    value = reader.GetString();
                }
            }
            return value;
        }

        // Null or unexpected token — skip gracefully
        if (reader.TokenType == JsonTokenType.StartArray)
            reader.Skip();

        return null;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}