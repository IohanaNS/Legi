namespace Legi.Identity.Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@bukihub.local";
    public string FromName { get; set; } = "BukiHub";
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// When no host is configured we fall back to logging the email instead of sending it,
    /// so the reset flow is fully exercisable in local development without an SMTP server.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
