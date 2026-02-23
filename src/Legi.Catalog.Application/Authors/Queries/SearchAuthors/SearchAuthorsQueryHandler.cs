using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Authors.Queries.SearchAuthors;

public class SearchAuthorsQueryHandler(IAuthorReadRepository authorReadRepository)
    : IRequestHandler<SearchAuthorsQuery, SearchAuthorsResponse>
{
    public async Task<SearchAuthorsResponse> Handle(
        SearchAuthorsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await authorReadRepository.SearchAsync(
            request.SearchTerm,
            request.Limit,
            cancellationToken);

        var authors = results
            .Select(a => new AuthorResult(a.Name, a.Slug, a.BooksCount))
            .ToList();

        return new SearchAuthorsResponse(authors);
    }
}
