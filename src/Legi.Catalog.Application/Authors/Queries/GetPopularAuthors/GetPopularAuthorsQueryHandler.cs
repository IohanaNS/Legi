using Legi.Catalog.Application.Authors.Queries.SearchAuthors;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Authors.Queries.GetPopularAuthors;

public class GetPopularAuthorsQueryHandler(IAuthorReadRepository authorReadRepository)
    : IRequestHandler<GetPopularAuthorsQuery, SearchAuthorsResponse>
{
    public async Task<SearchAuthorsResponse> Handle(
        GetPopularAuthorsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await authorReadRepository.GetPopularAsync(
            request.Limit,
            cancellationToken);

        var authors = results
            .Select(a => new AuthorResult(a.Name, a.Slug, a.BooksCount))
            .ToList();

        return new SearchAuthorsResponse(authors);
    }
}
