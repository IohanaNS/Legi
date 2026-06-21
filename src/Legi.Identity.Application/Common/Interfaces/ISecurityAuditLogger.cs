using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Application.Common.Interfaces;

/// <summary>
/// Records security-relevant events (logins, lockouts, password resets, account
/// deletion) to a durable, queryable audit trail. The default implementation emits
/// structured logs; it can be swapped for a database-backed store without touching
/// call sites.
/// </summary>
public interface ISecurityAuditLogger
{
    void Record(SecurityAuditEvent auditEvent);
}
