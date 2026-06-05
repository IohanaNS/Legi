using Legi.Library.Domain.ValueObjects;

namespace Legi.Library.Domain.Tests.Factories;

public static class RatingFactory
{
    public static Rating Create(int value = 8) => Rating.Create(value);

    public static Rating FromStars(decimal stars = 4.0m) => Rating.FromStars(stars);
}
