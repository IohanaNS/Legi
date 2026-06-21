using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Infrastructure.Security;

/// <summary>
/// Emits security audit events as structured logs under a stable per-event EventId,
/// so a log sink can filter/alert on them (e.g. a spike in <c>LoginFailed</c>). Swap
/// this adapter for a database-backed store later without changing any call site.
/// </summary>
public sealed class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger) : ISecurityAuditLogger
{
    public void Record(SecurityAuditEvent auditEvent)
    {
        logger.Log(
            LogLevel.Information,
            new EventId((int)auditEvent.Type, auditEvent.Type.ToString()),
            "SecurityAudit {SecurityEvent} userId={UserId} identifier={Identifier} ip={IpAddress} detail={Detail}",
            auditEvent.Type,
            auditEvent.UserId,
            auditEvent.Identifier,
            auditEvent.IpAddress,
            auditEvent.Detail);
    }
}
