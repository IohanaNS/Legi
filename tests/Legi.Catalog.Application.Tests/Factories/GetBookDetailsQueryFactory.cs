using Legi.Catalog.Application.Books.Queries.GetBookDetails;

namespace Legi.Catalog.Application.Tests.Factories;

public static class GetBookDetailsQueryFactory
{
    private static readonly Guid DefaultBookId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static GetBookDetailsQuery Create(Guid? bookId = null)
    {
        return new GetBookDetailsQuery(bookId ?? DefaultBookId);
    }
}
