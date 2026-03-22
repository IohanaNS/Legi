namespace Legi.Social.Application.Common.DTOs;

/// <summary>
/// Context about interactable content, used as a "header" on comments/likes pages.
/// Maps directly from the enriched ContentSnapshot read model.
/// </summary>
public class ContentContextDto
{
    public string TargetType { get; init; } = null!;
    public Guid TargetId { get; init; }
    public Guid OwnerId { get; init; }
    public string OwnerUsername { get; init; } = null!;
    public string? OwnerAvatarUrl { get; init; }
    public string? BookTitle { get; init; }
    public string? BookAuthor { get; init; }
    public string? BookCoverUrl { get; init; }
    public string? ContentPreview { get; init; }
}