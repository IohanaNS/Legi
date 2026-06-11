namespace Legi.Social.Application.Common.Storage;

/// <summary>
/// Validates and normalizes an uploaded image into a safe, storable form.
/// Implementations decode the input (rejecting non-images), crop/resize to the
/// dimensions appropriate for <paramref name="kind"/>, strip metadata, and
/// re-encode to a single canonical format.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Throws <see cref="FluentValidation.ValidationException"/> (or a derived
    /// validation error) when the stream is not a valid, supported image.
    /// </summary>
    Task<ProcessedImage> ProcessAsync(
        Stream input,
        ProfileImageKind kind,
        CancellationToken cancellationToken);
}
