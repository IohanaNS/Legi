namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// A validated cover image fetched from a provider: the raw bytes plus the
/// content type and file extension derived from the decoded image (not from the
/// provider's headers, which can lie). Only ever constructed for an image that
/// passed validation, so reaching this type means "this is a real cover."
/// </summary>
public sealed record CoverImage(byte[] Bytes, string ContentType, string Extension);
