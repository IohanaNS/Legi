using Legi.Identity.Application.Auth.Commands.BeginEmailMfaSetup;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Mfa;

public class BeginEmailMfaSetupCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMfaEmailCodeRepository> _codeRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly MfaSettings _mfaSettings = new() { EmailCodeLifetimeMinutes = 10 };

    private BeginEmailMfaSetupCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _codeRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _emailSenderMock.Object,
        _mfaSettings);

    [Fact]
    public async Task Handle_IssuesCode_AndEmailsIt_WhenEmailConfirmed()
    {
        var user = UserFactory.Create(emailConfirmed: true);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");

        await CreateHandler().Handle(new BeginEmailMfaSetupCommand(user.Id, "en"), CancellationToken.None);

        _codeRepositoryMock.Verify(x => x.IssueAsync(
            It.Is<MfaEmailCode>(c => c.UserId == user.Id && !c.IsConsumed),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(x => x.SendAsync(
            user.Email.Value, It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenMfaAlreadyEnabled()
    {
        var user = UserFactory.Create();
        user.EnableEmailMfa(["rec"], DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new BeginEmailMfaSetupCommand(user.Id, null), CancellationToken.None));

        _emailSenderMock.Verify(x => x.SendAsync(
            It.IsAny<string>(), It.IsAny<EmailContent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailNotConfirmed()
    {
        var user = UserFactory.Create(emailConfirmed: false);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new BeginEmailMfaSetupCommand(user.Id, null), CancellationToken.None));

        _codeRepositoryMock.Verify(x => x.IssueAsync(
            It.IsAny<MfaEmailCode>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
