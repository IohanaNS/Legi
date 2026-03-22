namespace Legi.Social.Application.Common.DTOs;

public class CommentDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string Content { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}