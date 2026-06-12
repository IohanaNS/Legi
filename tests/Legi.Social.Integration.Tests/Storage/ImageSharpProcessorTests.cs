using FluentValidation;
using Legi.Social.Application.Common.Storage;
using Legi.Social.Infrastructure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Legi.Social.Integration.Tests.Storage;

public class ImageSharpProcessorTests
{
    private readonly ImageSharpProcessor _processor = new();

    [Fact]
    public async Task ProcessAsync_PngImage_ReturnsWebpAvatar()
    {
        await using var input = await CreatePngImageAsync();

        var result = await _processor.ProcessAsync(input, ProfileImageKind.Avatar, CancellationToken.None);

        Assert.Equal("image/webp", result.ContentType);
        Assert.Equal("webp", result.Extension);
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    public async Task ProcessAsync_UnsupportedDecodedImageFormat_ThrowsValidationException()
    {
        await using var input = await CreateGifImageAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _processor.ProcessAsync(input, ProfileImageKind.Banner, CancellationToken.None));
    }

    private static async Task<MemoryStream> CreatePngImageAsync()
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(32, 32, Color.Green);
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateGifImageAsync()
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(32, 32, Color.Green);
        await image.SaveAsGifAsync(stream);
        stream.Position = 0;
        return stream;
    }
}
