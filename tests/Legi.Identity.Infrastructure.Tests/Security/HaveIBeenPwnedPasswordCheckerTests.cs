using System.Net;
using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class HaveIBeenPwnedPasswordCheckerTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    private static (string Prefix, string Suffix) Hash(string password)
    {
        var hex = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        return (hex[..5], hex[5..]);
    }

    private static HaveIBeenPwnedPasswordChecker CreateChecker(StubHandler handler, bool enabled = true)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.pwnedpasswords.com/") };
        var settings = new BreachedPasswordSettings { Enabled = enabled };
        return new HaveIBeenPwnedPasswordChecker(
            client, settings, NullLogger<HaveIBeenPwnedPasswordChecker>.Instance);
    }

    [Fact]
    public async Task IsBreachedAsync_ReturnsTrue_WhenSuffixPresentWithPositiveCount()
    {
        var (prefix, suffix) = Hash("password");
        var body = $"0000000000000000000000000000000000:0\n{suffix}:42\n";
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        var checker = CreateChecker(handler);

        var result = await checker.IsBreachedAsync("password");

        Assert.True(result);
        // Privacy guarantee: only the 5-char prefix is sent — never the suffix or full hash.
        Assert.Equal($"range/{prefix}", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.DoesNotContain(suffix, handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task IsBreachedAsync_ReturnsFalse_WhenSuffixAbsent()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:5\n") });
        var checker = CreateChecker(handler);

        Assert.False(await checker.IsBreachedAsync("a-fairly-unique-password"));
    }

    [Fact]
    public async Task IsBreachedAsync_ReturnsFalse_WhenSuffixHasZeroCount()
    {
        // Padded (fake) entries from the Add-Padding feature carry a count of 0.
        var (_, suffix) = Hash("padded-entry");
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{suffix}:0\n") });
        var checker = CreateChecker(handler);

        Assert.False(await checker.IsBreachedAsync("padded-entry"));
    }

    [Fact]
    public async Task IsBreachedAsync_FailsOpen_OnHttpError()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("network down"));
        var checker = CreateChecker(handler);

        Assert.False(await checker.IsBreachedAsync("password"));
    }

    [Fact]
    public async Task IsBreachedAsync_ReturnsFalse_WhenDisabled_WithoutCallingApi()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("HTTP must not be called when disabled"));
        var checker = CreateChecker(handler, enabled: false);

        Assert.False(await checker.IsBreachedAsync("password"));
        Assert.Null(handler.LastRequest);
    }
}
