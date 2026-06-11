namespace Legi.Social.Application.Common.Storage;

/// <summary>
/// The result of validating, normalizing and re-encoding an uploaded image.
/// Bytes are the final, safe-to-store payload (EXIF stripped, re-encoded).
/// </summary>
public sealed record ProcessedImage(byte[] Bytes, string ContentType, string Extension);
