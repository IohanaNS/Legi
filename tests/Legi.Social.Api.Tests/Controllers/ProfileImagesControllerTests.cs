using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Api.Controllers;
using Legi.Social.Application.Common.Storage;
using Legi.Social.Application.Profiles.Commands.SetProfileImage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Legi.Social.Api.Tests.Controllers;

public class ProfileImagesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IImageProcessor> _imageProcessor = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();

    private ProfileImagesController CreateController(Guid userId)
    {
        var controller = new ProfileImagesController(
            _mediator.Object,
            _imageProcessor.Object,
            _objectStorage.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                ], "test"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task UploadAvatar_ValidImage_UsesAuthenticatedUserAndReturnsUrl()
    {
        var userId = Guid.NewGuid();
        var processed = new ProcessedImage([1, 2, 3], "image/webp", "webp");
        var file = CreateFile("avatar.png", "image/png", 128);
        var url = $"/media/avatars/{userId}/new.webp";
        var controller = CreateController(userId);

        _imageProcessor
            .Setup(p => p.ProcessAsync(It.IsAny<Stream>(), ProfileImageKind.Avatar, It.IsAny<CancellationToken>()))
            .ReturnsAsync(processed);
        _objectStorage
            .Setup(s => s.PutProfileImageAsync(userId, ProfileImageKind.Avatar, processed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(url);
        _mediator
            .Setup(m => m.Send(
                It.Is<SetProfileImageCommand>(c =>
                    c.UserId == userId &&
                    c.Kind == ProfileImageKind.Avatar &&
                    c.Url == url),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SetProfileImageResponse(url));

        var result = await controller.UploadAvatar(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SetProfileImageResponse>(ok.Value);
        Assert.Equal(url, response.Url);
    }

    [Fact]
    public async Task UploadBanner_ValidImage_UsesBannerStorageAndReturnsUrl()
    {
        var userId = Guid.NewGuid();
        var processed = new ProcessedImage([1, 2, 3], "image/webp", "webp");
        var file = CreateFile("banner.webp", "image/webp", 128);
        var url = $"/media/banners/{userId}/new.webp";
        var controller = CreateController(userId);

        _imageProcessor
            .Setup(p => p.ProcessAsync(It.IsAny<Stream>(), ProfileImageKind.Banner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(processed);
        _objectStorage
            .Setup(s => s.PutProfileImageAsync(userId, ProfileImageKind.Banner, processed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(url);
        _mediator
            .Setup(m => m.Send(
                It.Is<SetProfileImageCommand>(c =>
                    c.UserId == userId &&
                    c.Kind == ProfileImageKind.Banner &&
                    c.Url == url),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SetProfileImageResponse(url));

        var result = await controller.UploadBanner(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SetProfileImageResponse>(ok.Value);
        Assert.Equal(url, response.Url);
    }

    [Fact]
    public async Task UploadAvatar_MissingFile_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());

        var result = await controller.UploadAvatar(null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _imageProcessor.Verify(
            p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<ProfileImageKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_EmptyFile_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());
        var file = CreateFile("empty.png", "image/png", 0);

        var result = await controller.UploadAvatar(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _imageProcessor.Verify(
            p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<ProfileImageKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_UnsupportedContentType_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());
        var file = CreateFile("avatar.gif", "image/gif", 128);

        var result = await controller.UploadAvatar(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _imageProcessor.Verify(
            p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<ProfileImageKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_OversizedFile_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());
        var file = CreateFile("avatar.png", "image/png", 2 * 1024 * 1024 + 1);

        var result = await controller.UploadAvatar(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _imageProcessor.Verify(
            p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<ProfileImageKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static FormFile CreateFile(string fileName, string contentType, int length)
    {
        var bytes = new byte[length];
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
