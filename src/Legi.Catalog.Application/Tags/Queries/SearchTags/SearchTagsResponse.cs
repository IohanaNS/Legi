namespace Legi.Catalog.Application.Tags.Queries.SearchTags;

public record SearchTagsResponse(List<TagResult> Tags);

public record TagResult(string Name, string Slug, int UsageCount);
