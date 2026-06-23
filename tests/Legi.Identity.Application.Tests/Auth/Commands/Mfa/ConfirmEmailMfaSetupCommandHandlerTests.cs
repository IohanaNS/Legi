using FluentValidation;
using Legi.Identity.Application.Auth.Commands.ConfirmEmailMfaSetup;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Mfa;

public class ConfirmEmailMfaSetupCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMfaEmailCodeRepository> _codeRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();

    private ConfirmEmailMfaSetupCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _codeRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _auditLoggerMock.Object);

    [Fact]
    public async Task Handle_EnablesEmailMfa_ConsumesCode_StoresRecoveryCodes_AndAudits()
    {
        var now = DateTime.UtcNow;
        var user = UserFactory.Create(emailConfirmed: true);
        var code = MfaEmailCode.Issue(user.Id, "h:123456", now.AddMinutes(10), now);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(code);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");

        var result = await CreateHandler().Handle(
            new ConfirmEmailMfaSetupCommand(user.Id, "123456"), CancellationToken.None);

        Assert.True(user.MfaEnabled);
        Assert.Equal(MfaMethod.Email, user.MfaMethod);
        Assert.Null(user.TotpSecret);
        Assert.True(code.IsConsumed);
        Assert.Equal(MfaRecoveryCodeGenerator.Count, result.RecoveryCodes.Count);
        Assert.Equal(MfaRecoveryCodeGenerator.Count, user.MfaRecoveryCodes.Count);
        _auditLoggerMock.Verify(x => x.Record(
            It.Is<SecurityAuditEvent>(e => e.Type == SecurityEventType.MfaEnabled
                                           && e.UserId == user.Id
                                           && e.Detail == "email")), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_AndRegistersFailedAttempt_WhenCodeWrong()
    {
        var now = DateTime.UtcNow;
        var user = UserFactory.Create(emailConfirmed: true);
        var code = MfaEmailCode.Issue(user.Id, "h:999999", now.AddMinutes(10), now);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(code);
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"h:{s}");

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new ConfirmEmailMfaSetupCommand(user.Id, "123456"), CancellationToken.None));

        Assert.False(user.MfaEnabled);
        Assert.Equal(1, code.AttemptCount);
        _codeRepositoryMock.Verify(x => x.UpdateAsync(code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenNoActiveCode()
    {
        var user = UserFactory.Create(emailConfirmed: true);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaEmailCode?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new ConfirmEmailMfaSetupCommand(user.Id, "123456"), CancellationToken.None));

        Assert.False(user.MfaEnabled);
    }

    [Fact]
    public async Task Handle_Throws_WhenMfaAlreadyEnabled()
    {
        var user = UserFactory.Create();
        user.EnableEmailMfa(["rec"], DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new ConfirmEmailMfaSetupCommand(user.Id, "123456"), CancellationToken.None));
    }
}
