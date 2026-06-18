using Legi.Catalog.Application.Common.Storage;
using Legi.SharedKernel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Decodes a user-uploaded cover, enforces a minimum size, downscales oversized
/// uploads (preserving aspect — covers aren't a fixed ratio), and re-encodes to
/// WebP. Re-encoding strips EXIF and neutralizes payloads disguised as images.
/// </summary>
public sealed class ImageSharpCoverProcessor : IBookCoverImageProcessor
{
    private const int MinDimension = 100;
    private const int MaxWidth = 1000;

    public async Task<CoverImage> ProcessAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        Image image;
        try
        {
            image = await Image.LoadAsync(imageStream, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new DomainException("The uploaded file is not a valid image.");
        }

        using (image)
        {
            if (image.Width < MinDimension || image.Height < MinDimension)
                throw new DomainException($"Cover image must be at least {MinDimension}x{MinDimension} pixels.");

            if (image.Width > MaxWidth)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(MaxWidth, 0), // height auto-scales to preserve aspect
                    Mode = ResizeMode.Max
                }));
            }

            using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);

            return new CoverImage(output.ToArray(), "image/webp", "webp");
        }
    }
}
