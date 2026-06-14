using Legi.Identity.Application.Common.Email;

namespace Legi.Identity.Application.Tests.Common.Email;

public class PasswordResetEmailTemplateTests
{
    private const string Url = "https://bukihub.test/reset-password?token=abc";

    [Fact]
    public void Build_DefaultsToEnglish_WhenLanguageIsNull()
    {
        var content = PasswordResetEmailTemplate.Build("alice", Url, 60, null);

        Assert.Equal("Reset your BukiHub password", content.Subject);
        Assert.Contains("Reset password", content.HtmlBody);
        Assert.Contains("The BukiHub team", content.TextBody);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("pt")]
    [InlineData("PT-br")]
    public void Build_UsesPortuguese_ForPortugueseLanguages(string language)
    {
        var content = PasswordResetEmailTemplate.Build("alice", Url, 60, language);

        Assert.Equal("Redefina sua senha do BukiHub", content.Subject);
        Assert.Contains("Redefinir senha", content.HtmlBody);
        Assert.Contains("Equipe BukiHub", content.TextBody);
    }

    [Fact]
    public void Build_FallsBackToEnglish_ForUnknownLanguage()
    {
        var content = PasswordResetEmailTemplate.Build("alice", Url, 60, "fr");

        Assert.Equal("Reset your BukiHub password", content.Subject);
    }

    [Fact]
    public void Build_IncludesUrlInBothBodies_AndExpiry()
    {
        var content = PasswordResetEmailTemplate.Build("alice", Url, 45, "en");

        Assert.Contains(Url, content.HtmlBody);
        Assert.Contains(Url, content.TextBody);
        Assert.Contains("45 minutes", content.HtmlBody);
    }

    [Fact]
    public void Build_HtmlEncodesUsername()
    {
        var content = PasswordResetEmailTemplate.Build("<b>x</b>", Url, 60, "en");

        Assert.DoesNotContain("<b>x</b>", content.HtmlBody);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", content.HtmlBody);
    }

    [Fact]
    public void Build_EmbedsLogoInlineAndReferencesItByCid()
    {
        var content = PasswordResetEmailTemplate.Build("alice", Url, 60, "en");

        Assert.NotNull(content.InlineImages);
        var logo = Assert.Single(content.InlineImages!);
        Assert.Equal("image/png", logo.MediaType);
        Assert.NotEmpty(logo.Content);
        Assert.Contains($"cid:{logo.ContentId}", content.HtmlBody);
    }
}
