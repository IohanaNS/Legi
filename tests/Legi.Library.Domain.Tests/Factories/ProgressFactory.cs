using Legi.Library.Domain.ValueObjects;

namespace Legi.Library.Domain.Tests.Factories;

public static class ProgressFactory
{
    public static Progress Percentage(int value = 50) => Progress.CreatePercentage(value);

    public static Progress Page(int value = 120) => Progress.CreatePage(value);
}
