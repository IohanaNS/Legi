namespace Legi.Identity.Application.Common.Interfaces;

public interface IHumanVerificationService
{
    Task<bool> VerifyAsync(
        string? token,
        string? remoteIpAddress,
        CancellationToken cancellationToken = default);
}
