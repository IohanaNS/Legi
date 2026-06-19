namespace Legi.Identity.Application.Common.Email;

public static class EmailConfirmationEmailTemplate
{
    public static EmailContent Build(
        string username,
        string confirmationUrl,
        int lifetimeMinutes,
        string? language = null)
    {
        return BukiHubActionEmailTemplate.Build(
            Strings.Resolve(language),
            username,
            confirmationUrl,
            lifetimeMinutes);
    }

    private static class Strings
    {
        public static ActionEmailStrings Resolve(string? language)
        {
            var lang = language?.Trim().ToLowerInvariant() ?? string.Empty;

            if (lang.StartsWith("pt"))
                return Portuguese;

            return English;
        }

        private static readonly ActionEmailStrings English = new(
            LangAttr: "en",
            Subject: "Confirm your BukiHub email",
            Heading: "Confirm your email",
            Greeting: "Hi {0},",
            Intro: "Your BukiHub account is ready. Click the button below to confirm this email address and finish signing in.",
            Button: "Confirm email",
            ExpiryNote: "This link expires in {0} minutes and can only be used once. If the button doesn't work, copy and paste this link into your browser:",
            Disclaimer: "If you didn't create a BukiHub account, you can safely ignore this email.",
            TextIntro: "Your BukiHub account is ready. Open the link below to confirm this email address and finish signing in.",
            TextExpiry: "It expires in {0} minutes and can only be used once:",
            Signoff: "— The BukiHub team");

        private static readonly ActionEmailStrings Portuguese = new(
            LangAttr: "pt-BR",
            Subject: "Confirme seu e-mail do BukiHub",
            Heading: "Confirme seu e-mail",
            Greeting: "Olá {0},",
            Intro: "Sua conta BukiHub está pronta. Clique no botão abaixo para confirmar este e-mail e concluir o acesso.",
            Button: "Confirmar e-mail",
            ExpiryNote: "Este link expira em {0} minutos e só pode ser usado uma vez. Se o botão não funcionar, copie e cole este link no seu navegador:",
            Disclaimer: "Se você não criou uma conta BukiHub, pode ignorar este e-mail com segurança.",
            TextIntro: "Sua conta BukiHub está pronta. Abra o link abaixo para confirmar este e-mail e concluir o acesso.",
            TextExpiry: "Ele expira em {0} minutos e só pode ser usado uma vez:",
            Signoff: "— Equipe BukiHub");
    }
}
