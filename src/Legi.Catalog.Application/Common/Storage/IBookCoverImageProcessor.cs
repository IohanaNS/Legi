namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// Decodes and normalizes a user-uploaded cover image into a storable
/// <see cref="CoverImage"/> (validate it decodes, enforce minimum dimensions,
/// downscale oversized uploads). Throws a domain exception for anything that
/// isn't a usable image. Implemented in Infrastructure (ImageSharp).
/// </summary>
public interface IBookCoverImageProcessor
{
    Task<CoverImage> ProcessAsync(Stream imageStream, CancellationToken cancellationToken);
}
