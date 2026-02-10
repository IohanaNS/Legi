using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Application.Tests.Factories;

public static class DomainBookFactory
{
    private static readonly Guid DefaultCreatedByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Book Create(
        string isbn = "9780132350884",
        string title = "Clean Code")
    {
        return Book.Create(
            Isbn.Create(isbn),
            title,
            [Author.Create("Robert C. Martin")],
            DefaultCreatedByUserId
        );
    }
}
