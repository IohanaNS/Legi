using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Common;

/// <summary>
/// Issues a fresh one-time email code for a user (superseding any previous one) and
/// emails it. Shared by email-MFA enrollment and the login challenge so the issue/send
/// logic lives in one place.
/// </summary>
internal static class MfaEmailCodeDispatcher
{
    public static async Task DispatchAsync(
        User user,
        string? language,
        IMfaEmailCodeRepository codeRepository,
        ISecureTokenFactory tokenFactory,
        IEmailSender emailSender,
        int lifetimeMinutes,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var code = MfaEmailCodeGenerator.Generate();
        var hash = tokenFactory.Hash(MfaEmailCodeGenerator.Normalize(code));

        var entity = MfaEmailCode.Issue(user.Id, hash, now.AddMinutes(lifetimeMinutes), now);
        await codeRepository.IssueAsync(entity, cancellationToken);

        var content = MfaCodeEmailTemplate.Build(user.Username.Value, code, lifetimeMinutes, language);
        await emailSender.SendAsync(user.Email.Value, content, cancellationToken);
    }
}
