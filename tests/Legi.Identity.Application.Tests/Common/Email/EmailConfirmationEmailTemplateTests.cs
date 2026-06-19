using Legi.Identity.Application.Common.Email;

namespace Legi.Identity.Application.Tests.Common.Email;

public class EmailConfirmationEmailTemplateTests
{
    private const string Url = "https://bukihub.test/confirm-email?token=abc";

    [Fact]
    public void Build_DefaultsToEnglish_WhenLanguageIsNull()
    {
        var content = EmailConfirmationEmailTemplate.Build("alice", Url, 1440, null);

        Assert.Equal("Confirm your BukiHub email", content.Subject);
        Assert.Contains("Confirm email", content.HtmlBody);
        Assert.Contains("The BukiHub team", content.TextBody);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("pt")]
    [InlineData("PT-br")]
    public void Build_UsesPortuguese_ForPortugueseLanguages(string language)
    {
        var content = EmailConfirmationEmailTemplate.Build("alice", Url, 1440, language);

        Assert.Equal("Confirme seu e-mail do BukiHub", content.Subject);
        Assert.Contains("Confirmar e-mail", content.HtmlBody);
        Assert.Contains("Equipe BukiHub", content.TextBody);
    }

    [Fact]
    public void Build_FallsBackToEnglish_ForUnknownLanguage()
    {
        var content = EmailConfirmationEmailTemplate.Build("alice", Url, 1440, "fr");

        Assert.Equal("Confirm your BukiHub email", content.Subject);
    }

    [Fact]
    public void Build_IncludesUrlInBothBodies_AndExpiry()
    {
        var content = EmailConfirmationEmailTemplate.Build("alice", Url, 45, "en");

        Assert.Contains(Url, content.HtmlBody);
        Assert.Contains(Url, content.TextBody);
        Assert.Contains("45 minutes", content.HtmlBody);
    }

    [Fact]
    public void Build_HtmlEncodesUsername()
    {
        var content = EmailConfirmationEmailTemplate.Build("<b>x</b>", Url, 1440, "en");

        Assert.DoesNotContain("<b>x</b>", content.HtmlBody);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", content.HtmlBody);
    }

    [Fact]
    public void Build_EmbedsLogoInlineAndReferencesItByCid()
    {
        var content = EmailConfirmationEmailTemplate.Build("alice", Url, 1440, "en");

        Assert.NotNull(content.InlineImages);
        var logo = Assert.Single(content.InlineImages!);
        Assert.Equal("image/png", logo.MediaType);
        Assert.NotEmpty(logo.Content);
        Assert.Contains($"cid:{logo.ContentId}", content.HtmlBody);
    }
}
