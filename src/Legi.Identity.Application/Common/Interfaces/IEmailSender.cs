using Legi.Identity.Application.Common.Email;

namespace Legi.Identity.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, EmailContent content, CancellationToken cancellationToken = default);
}
