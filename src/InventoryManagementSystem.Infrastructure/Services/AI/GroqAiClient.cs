using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

public sealed class GroqAiClient
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;

    public GroqAiClient(
        HttpClient httpClient,
        IOptions<GroqSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<string> GenerateAsync(
        string instructions,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = instructions },
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.ApiKey);

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Groq request failed with HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var choices = document.RootElement.GetProperty("choices");

        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Groq returned no response choices.");
        }

        string? content = choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Groq returned an empty response.");
        }

        return content;
    }
}
