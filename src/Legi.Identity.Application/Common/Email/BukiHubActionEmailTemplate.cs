using System.Net;
using System.Reflection;

namespace Legi.Identity.Application.Common.Email;

internal static class BukiHubActionEmailTemplate
{
    private const string BrandName = "BukiHub";
    private const string BrandGreen = "#15803d";
    private const string Tagline = "YOUR BOOKS, YOUR SPACE";
    private const string TaglineColor = "#a99a7c";
    private const string TextColor = "#292524";
    private const string MutedColor = "#78716c";
    private const string PageBg = "#faf6ef";
    private const string CardBg = "#ffffff";
    private const string BorderColor = "#e7e2d6";

    private const string LogoContentId = "bukihub-mark";
    private static readonly Lazy<byte[]> LogoBytes = new(LoadLogoBytes);

    public static EmailContent Build(
        ActionEmailStrings s,
        string username,
        string actionUrl,
        int lifetimeMinutes)
    {
        var safeName = WebUtility.HtmlEncode(username);
        var safeUrl = WebUtility.HtmlEncode(actionUrl);

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
                         <td align="center" style="padding-bottom:24px;">
                           <table role="presentation" cellpadding="0" cellspacing="0">
                             <tr>
                               <td style="padding-right:12px; vertical-align:middle;">
                                 <img src="cid:{LogoContentId}" width="34" height="63" alt="{BrandName}" style="display:block; border:0;">
                               </td>
                               <td style="vertical-align:middle;">
                                 <div style="font-family:Georgia,'Times New Roman',serif; font-size:28px; font-weight:bold; color:{BrandGreen}; line-height:1;">{BrandName}</div>
                                 <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif; font-size:10px; letter-spacing:2px; color:{TaglineColor}; padding-top:6px;">{Tagline}</div>
                               </td>
                             </tr>
                           </table>
                         </td>
                       </tr>
                       <tr>
                         <td style="background-color:{CardBg}; border:1px solid {BorderColor}; border-radius:12px; padding:32px;">
                           <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                             <tr>
                               <td style="font-size:20px; font-weight:600; color:{TextColor}; padding-bottom:16px;">
                                 {s.Heading}
                               </td>
                             </tr>
                             <tr>
                               <td style="font-size:15px; line-height:1.6; color:{TextColor}; padding-bottom:12px;">
                                 {string.Format(s.Greeting, safeName)}
                               </td>
                             </tr>
                             <tr>
                               <td style="font-size:15px; line-height:1.6; color:{TextColor}; padding-bottom:24px;">
                                 {s.Intro}
                               </td>
                             </tr>
                             <tr>
                               <td align="center" style="padding-bottom:24px;">
                                 <a href="{safeUrl}" style="display:inline-block; background-color:{BrandGreen}; color:#ffffff; font-size:15px; font-weight:600; text-decoration:none; padding:13px 28px; border-radius:8px;">{s.Button}</a>
                               </td>
                             </tr>
                             <tr>
                               <td style="font-size:13px; line-height:1.6; color:{MutedColor}; padding-bottom:20px;">
                                 {string.Format(s.ExpiryNote, lifetimeMinutes)}<br>
                                 <a href="{safeUrl}" style="color:{BrandGreen}; word-break:break-all;">{safeUrl}</a>
                               </td>
                             </tr>
                             <tr>
                               <td style="font-size:13px; line-height:1.6; color:{MutedColor}; border-top:1px solid {BorderColor}; padding-top:20px;">
                                 {s.Disclaimer}
                               </td>
                             </tr>
                           </table>
                         </td>
                       </tr>
                       <tr>
                         <td align="center" style="padding-top:24px; font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif; font-size:12px; color:{MutedColor};">
                           &copy; {DateTime.UtcNow.Year} {BrandName}
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

             {s.TextIntro}
             {string.Format(s.TextExpiry, lifetimeMinutes)}

             {actionUrl}

             {s.Disclaimer}

             {s.Signoff}
             """;

        var images = new[] { new InlineImage(LogoContentId, "image/png", LogoBytes.Value) };

        return new EmailContent(s.Subject, html, text, images);
    }

    private static byte[] LoadLogoBytes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("bukihub-mark.png", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

internal sealed record ActionEmailStrings(
    string LangAttr,
    string Subject,
    string Heading,
    string Greeting,
    string Intro,
    string Button,
    string ExpiryNote,
    string Disclaimer,
    string TextIntro,
    string TextExpiry,
    string Signoff);
