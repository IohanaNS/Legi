namespace Legi.Identity.Application.Common.Models;

public class MfaSettings
{
    public const string SectionName = "Mfa";

    /// <summary>
    /// Base64-encoded 32-byte (256-bit) key used to encrypt TOTP secrets at rest
    /// (AES-256-GCM). Required only once MFA is in use. Generate with:
    /// <c>openssl rand -base64 32</c>.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>Issuer shown in the authenticator app.</summary>
    public string Issuer { get; set; } = "BukiHub";
}
