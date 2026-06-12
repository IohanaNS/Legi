using System.Text.Json;
using System.Text.Json.Serialization;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Infrastructure.Security;

public sealed class TurnstileVerificationService : IHumanVerificationService
{
    private readonly TurnstileSettings _settings;
    private readonly HttpClient _httpClient;

    public TurnstileVerificationService(
        TurnstileSettings settings,
        HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task<bool> VerifyAsync(
        string? token,
        string? remoteIpAddress,
        string expectedAction,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (string.IsNullOrWhiteSpace(expectedAction))
            return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
            form["remoteip"] = remoteIpAddress;

        try
        {
            using var content = new FormUrlEncodedContent(form);
            using var response = await _httpClient.PostAsync(
                _settings.SiteVerifyUrl,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<TurnstileVerifyResponse>(
                stream,
                cancellationToken: cancellationToken);

            return payload?.Success == true &&
                   string.Equals(payload.Action, expectedAction, StringComparison.Ordinal) &&
                   _settings.AllowedHostnames.Any(hostname =>
                       string.Equals(hostname.Trim(), payload.Hostname, StringComparison.OrdinalIgnoreCase));
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }
    }
}
