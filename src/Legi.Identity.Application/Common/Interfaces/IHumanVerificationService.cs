namespace Legi.Identity.Application.Common.Interfaces;

public interface IHumanVerificationService
{
    Task<bool> VerifyAsync(
        string? token,
        string? remoteIpAddress,
        string expectedAction,
        CancellationToken cancellationToken = default);
}
