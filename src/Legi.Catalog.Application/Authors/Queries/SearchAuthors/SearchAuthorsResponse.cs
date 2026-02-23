namespace Legi.Catalog.Application.Authors.Queries.SearchAuthors;

public record SearchAuthorsResponse(List<AuthorResult> Authors);

public record AuthorResult(string Name, string Slug, int BooksCount);
