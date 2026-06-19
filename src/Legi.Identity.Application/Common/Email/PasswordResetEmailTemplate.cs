namespace Legi.Identity.Application.Common.Email;

/// <summary>
/// Builds the password-reset email. Uses a table-based, inline-styled layout (the only thing
/// email clients render reliably), embeds the BukiHub mark inline via Content-ID, and always
/// pairs the HTML with a plain-text alternative.
/// Localized from the language the frontend passes through (falls back to English).
/// </summary>
public static class PasswordResetEmailTemplate
{
    public static EmailContent Build(string username, string resetUrl, int lifetimeMinutes, string? language = null)
    {
        return BukiHubActionEmailTemplate.Build(
            Strings.Resolve(language),
            username,
            resetUrl,
            lifetimeMinutes);
    }

    private static class Strings
    {
        public static ActionEmailStrings Resolve(string? language)
        {
            // Match by primary subtag, so "pt", "pt-BR", "pt_BR" all map to Portuguese.
            var lang = language?.Trim().ToLowerInvariant() ?? string.Empty;

            if (lang.StartsWith("pt"))
                return Portuguese;

            return English;
        }

        private static readonly ActionEmailStrings English = new(
            LangAttr: "en",
            Subject: "Reset your BukiHub password",
            Heading: "Reset your password",
            Greeting: "Hi {0},",
            Intro: "We received a request to reset your BukiHub password. Click the button below to choose a new one.",
            Button: "Reset password",
            ExpiryNote: "This link expires in {0} minutes and can only be used once. If the button doesn't work, copy and paste this link into your browser:",
            Disclaimer: "If you didn't request this, you can safely ignore this email — your password won't change.",
            TextIntro: "We received a request to reset your BukiHub password. Open the link below to choose a new one.",
            TextExpiry: "It expires in {0} minutes and can only be used once:",
            Signoff: "— The BukiHub team");

        private static readonly ActionEmailStrings Portuguese = new(
            LangAttr: "pt-BR",
            Subject: "Redefina sua senha do BukiHub",
            Heading: "Redefina sua senha",
            Greeting: "Olá {0},",
            Intro: "Recebemos uma solicitação para redefinir sua senha do BukiHub. Clique no botão abaixo para escolher uma nova.",
            Button: "Redefinir senha",
            ExpiryNote: "Este link expira em {0} minutos e só pode ser usado uma vez. Se o botão não funcionar, copie e cole este link no seu navegador:",
            Disclaimer: "Se você não solicitou isso, pode ignorar este e-mail com segurança — sua senha não será alterada.",
            TextIntro: "Recebemos uma solicitação para redefinir sua senha do BukiHub. Abra o link abaixo para escolher uma nova.",
            TextExpiry: "Ele expira em {0} minutos e só pode ser usado uma vez:",
            Signoff: "— Equipe BukiHub");
    }
}
