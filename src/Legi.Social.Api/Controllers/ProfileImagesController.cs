using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Storage;
using Legi.Social.Application.Profiles.Commands.SetProfileImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social/users/me")]
[Authorize]
public class ProfileImagesController : ControllerBase
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;  // 2 MB
    private const long MaxBannerBytes = 5 * 1024 * 1024;  // 5 MB

    private readonly IMediator _mediator;
    private readonly IImageProcessor _imageProcessor;
    private readonly IObjectStorage _objectStorage;

    public ProfileImagesController(
        IMediator mediator,
        IImageProcessor imageProcessor,
        IObjectStorage objectStorage)
    {
        _mediator = mediator;
        _imageProcessor = imageProcessor;
        _objectStorage = objectStorage;
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(MaxAvatarBytes)]
    public Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
        => UploadAsync(file, ProfileImageKind.Avatar, MaxAvatarBytes, cancellationToken);

    [HttpPost("banner")]
    [RequestSizeLimit(MaxBannerBytes)]
    public Task<IActionResult> UploadBanner(IFormFile file, CancellationToken cancellationToken)
        => UploadAsync(file, ProfileImageKind.Banner, MaxBannerBytes, cancellationToken);

    private async Task<IActionResult> UploadAsync(
        IFormFile file,
        ProfileImageKind kind,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A non-empty image file is required." });

        if (file.Length > maxBytes)
            return BadRequest(new { error = $"Image must not exceed {maxBytes / (1024 * 1024)} MB." });

        var userId = GetUserId();

        await using var stream = file.OpenReadStream();
        var processed = await _imageProcessor.ProcessAsync(stream, kind, cancellationToken);
        var url = await _objectStorage.PutProfileImageAsync(userId, kind, processed, cancellationToken);

        var result = await _mediator.Send(new SetProfileImageCommand(userId, kind, url), cancellationToken);
        return Ok(result);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());
}
