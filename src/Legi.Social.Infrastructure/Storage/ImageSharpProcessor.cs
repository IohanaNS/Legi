using FluentValidation;
using FluentValidation.Results;
using Legi.Social.Application.Common.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Legi.Social.Infrastructure.Storage;

/// <summary>
/// Decodes, crops/resizes and re-encodes uploads to WebP. Re-encoding strips
/// EXIF and neutralizes payloads disguised as images. Output dimensions are
/// fixed per <see cref="ProfileImageKind"/>.
/// </summary>
public sealed class ImageSharpProcessor : IImageProcessor
{
    private static readonly Size AvatarSize = new(512, 512);
    private static readonly Size BannerSize = new(1500, 500);
    private static readonly HashSet<string> AllowedInputMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<ProcessedImage> ProcessAsync(
        Stream input,
        ProfileImageKind kind,
        CancellationToken cancellationToken)
    {
        Image image;
        try
        {
            image = await Image.LoadAsync(input, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new ValidationException("The uploaded file is not a valid image.",
            [
                new ValidationFailure("File", "The uploaded file is not a valid image.")
            ]);
        }

        using (image)
        {
            if (!IsAllowedDecodedFormat(image.Metadata.DecodedImageFormat))
                throw new ValidationException("Only JPG, PNG, or WebP images are supported.",
                [
                    new ValidationFailure("File", "Only JPG, PNG, or WebP images are supported.")
                ]);

            var targetSize = kind == ProfileImageKind.Avatar ? AvatarSize : BannerSize;

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = targetSize,
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);

            return new ProcessedImage(output.ToArray(), "image/webp", "webp");
        }
    }

    private static bool IsAllowedDecodedFormat(IImageFormat? format)
        => format?.MimeTypes.Any(AllowedInputMimeTypes.Contains) == true;
}
