using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Legi.Identity.Infrastructure.Email;

public class SmtpEmailSender(SmtpSettings settings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, EmailContent content, CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            // Dev fallback: no SMTP configured, so log the message (and any reset link it carries)
            // instead of sending. Lets the full flow be exercised locally without a mail server.
            logger.LogWarning(
                "SMTP not configured; email to {ToEmail} not sent. Subject: {Subject}\n{Body}",
                toEmail, content.Subject, content.TextBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = content.Subject;

        var builder = new BodyBuilder { HtmlBody = content.HtmlBody, TextBody = content.TextBody };

        foreach (var image in content.InlineImages ?? [])
        {
            var resource = builder.LinkedResources.Add(
                image.ContentId,
                image.Content,
                ContentType.Parse(image.MediaType));
            resource.ContentId = image.ContentId;
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var socketOptions = settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
