using System.Net;

namespace Legi.Identity.Application.Common.Email;

/// <summary>
/// Builds the email that carries a one-time MFA code. Unlike the action emails
/// (password reset / confirmation) this shows a code to type rather than a button to
/// click, so it uses its own minimal, code-centric layout. Table-based + inline styles
/// (the only thing email clients render reliably), HTML paired with plain text, and
/// localized by primary subtag (falls back to English).
/// </summary>
public static class MfaCodeEmailTemplate
{
    private const string BrandGreen = "#15803d";
    private const string TextColor = "#292524";
    private const string MutedColor = "#78716c";
    private const string PageBg = "#faf6ef";
    private const string CardBg = "#ffffff";
    private const string BorderColor = "#e7e2d6";
    private const string CodeBg = "#f5f1e8";

    public static EmailContent Build(string username, string code, int lifetimeMinutes, string? language = null)
    {
        var s = Strings.Resolve(language);
        var safeName = WebUtility.HtmlEncode(username);

        var html =
            $"""
             <!DOCTYPE html>
             <html lang="{s.LangAttr}">
             <head>
               <meta charset="utf-8">
               <meta name="viewport" content="width=device-width, initial-scale=1.0">
               <meta name="color-scheme" content="light">
               <title>{s.Subject}</title>
             </head>
             <body style="margin:0; padding:0; background-color:{PageBg}; -webkit-text-size-adjust:100%;">
               <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{PageBg};">
                 <tr>
                   <td align="center" style="padding:32px 16px;">
                     <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px; width:100%;">
                       <tr>
                         <td align="center" style="padding-bottom:24px; font-family:Georgia,'Times New Roman',serif; font-size:28px; font-weight:bold; color:{BrandGreen};">
                           BukiHub
                         </td>
                       </tr>
                       <tr>
                         <td style="background-color:{CardBg}; border:1px solid {BorderColor}; border-radius:12px; padding:32px;">
                           <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                             <tr>
                               <td style="font-size:20px; font-weight:600; color:{TextColor}; padding-bottom:16px;">{s.Heading}</td>
                             </tr>
                             <tr>
                               <td style="font-size:15px; line-height:1.6; color:{TextColor}; padding-bottom:12px;">{string.Format(s.Greeting, safeName)}</td>
                             </tr>
                             <tr>
                               <td style="font-size:15px; line-height:1.6; color:{TextColor}; padding-bottom:24px;">{s.Intro}</td>
                             </tr>
                             <tr>
                               <td align="center" style="padding-bottom:24px;">
                                 <div style="display:inline-block; background-color:{CodeBg}; border:1px solid {BorderColor}; border-radius:8px; padding:16px 28px; font-family:'Courier New',Courier,monospace; font-size:32px; font-weight:bold; letter-spacing:8px; color:{TextColor};">{code}</div>
                               </td>
                             </tr>
                             <tr>
                               <td style="font-size:13px; line-height:1.6; color:{MutedColor};">{string.Format(s.ExpiryNote, lifetimeMinutes)}</td>
                             </tr>
                             <tr>
                               <td style="font-size:13px; line-height:1.6; color:{MutedColor}; border-top:1px solid {BorderColor}; padding-top:20px; margin-top:20px;">{s.Disclaimer}</td>
                             </tr>
                           </table>
                         </td>
                       </tr>
                       <tr>
                         <td align="center" style="padding-top:24px; font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif; font-size:12px; color:{MutedColor};">
                           &copy; {DateTime.UtcNow.Year} BukiHub
                         </td>
                       </tr>
                     </table>
                   </td>
                 </tr>
               </table>
             </body>
             </html>
             """;

        var text =
            $"""
             {string.Format(s.Greeting, username)}

             {s.Intro}

             {code}

             {string.Format(s.ExpiryNote, lifetimeMinutes)}

             {s.Disclaimer}

             {s.Signoff}
             """;

        return new EmailContent(s.Subject, html, text);
    }

    private static class Strings
    {
        public static MfaCodeStrings Resolve(string? language)
        {
            var lang = language?.Trim().ToLowerInvariant() ?? string.Empty;
            return lang.StartsWith("pt") ? Portuguese : English;
        }

        private static readonly MfaCodeStrings English = new(
            LangAttr: "en",
            Subject: "Your BukiHub verification code",
            Heading: "Your verification code",
            Greeting: "Hi {0},",
            Intro: "Use this code to finish signing in to BukiHub.",
            ExpiryNote: "This code expires in {0} minutes and can only be used once.",
            Disclaimer: "If you didn't try to sign in, someone may have your password — change it as soon as you can.",
            Signoff: "— The BukiHub team");

        private static readonly MfaCodeStrings Portuguese = new(
            LangAttr: "pt-BR",
            Subject: "Seu código de verificação do BukiHub",
            Heading: "Seu código de verificação",
            Greeting: "Olá {0},",
            Intro: "Use este código para concluir o acesso ao BukiHub.",
            ExpiryNote: "Este código expira em {0} minutos e só pode ser usado uma vez.",
            Disclaimer: "Se você não tentou entrar, alguém pode ter sua senha — troque-a assim que possível.",
            Signoff: "— Equipe BukiHub");
    }

    private sealed record MfaCodeStrings(
        string LangAttr,
        string Subject,
        string Heading,
        string Greeting,
        string Intro,
        string ExpiryNote,
        string Disclaimer,
        string Signoff);
}
