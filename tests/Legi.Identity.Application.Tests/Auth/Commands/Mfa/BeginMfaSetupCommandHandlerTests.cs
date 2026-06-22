using Legi.Identity.Application.Auth.Commands.BeginMfaSetup;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Mfa;

public class BeginMfaSetupCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IMfaSecretProtector> _secretProtectorMock = new();

    private BeginMfaSetupCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _totpServiceMock.Object,
        _secretProtectorMock.Object,
        new MfaSettings { Issuer = "BukiHub" });

    [Fact]
    public async Task Handle_StartsEnrollment_AndReturnsSecretAndUri()
    {
        var user = UserFactory.Create();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns("BASE32SECRET");
        _secretProtectorMock.Setup(x => x.Protect("BASE32SECRET")).Returns("encrypted");
        _totpServiceMock.Setup(x => x.BuildOtpAuthUri("BASE32SECRET", user.Email.Value, "BukiHub"))
            .Returns("otpauth://totp/x");

        var result = await CreateHandler().Handle(new BeginMfaSetupCommand(user.Id), CancellationToken.None);

        Assert.Equal("BASE32SECRET", result.Secret);
        Assert.Equal("otpauth://totp/x", result.OtpAuthUri);
        Assert.Equal("encrypted", user.TotpSecret); // stored, but not yet enabled
        Assert.False(user.MfaEnabled);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenMfaAlreadyEnabled()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("enc", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(["h"], DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new BeginMfaSetupCommand(user.Id), CancellationToken.None));
    }
}
