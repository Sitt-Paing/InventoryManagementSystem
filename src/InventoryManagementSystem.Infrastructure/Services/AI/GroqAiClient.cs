using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InventoryManagementSystem.Application.Common.Exceptions;
using InventoryManagementSystem.Application.Common.Interfaces;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

public sealed class GroqAiClient
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly ILogger<GroqAiClient> _logger;

    public GroqAiClient(
        HttpClient httpClient,
        IOptions<GroqSettings> options,
        ILogger<GroqAiClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        string instructions,
        string prompt,
        CancellationToken cancellationToken = default,
        bool jsonOutput = false)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = _settings.Model,
            ["messages"] = new[]
    {
        new { role = "system", content = instructions },
        new { role = "user", content = prompt }
    }
        };

        if (jsonOutput)
        {
            payload["response_format"] = new
            {
                type = "json_object"
            };
        }

        return ReadContent(await SendAsync(payload, cancellationToken));
    }

    public async Task<string> GenerateWithToolsAsync(
        string instructions,
        string prompt,
        IReadOnlyList<DashboardToolDefinitionDto> tools,
        Func<string, JsonElement, CancellationToken, Task<string>> executeTool,
        CancellationToken cancellationToken)
    {
        if (tools.Count != 1 || tools[0].Name != "GetDashboardFacts")
            throw new InvalidOperationException("The dashboard facts tool is unavailable.");

        var messages = new List<object>
        {
            new { role = "system", content = "Retrieve inventory evidence. Call GetDashboardFacts exactly once using the user's exact startDate and endDate. Do not write the dashboard yet." },
            new { role = "user", content = prompt }
        };
        var toolSchemas = tools.Select(tool => new
        {
            type = "function",
            function = new { name = tool.Name, description = tool.Description, parameters = tool.InputSchema }
        }).ToArray();

        // One read tool followed by one final response. Only a short final-turn
        // 429 may be retried once; tool selection/execution is never repeated.
        // A required tool on the first turn prevents a dashboard without evidence.
        var toolMessage = await SendAsync(new Dictionary<string, object>
        {
            ["model"] = _settings.Model,
            ["messages"] = messages,
            ["tools"] = toolSchemas,
            ["tool_choice"] = "required",
            ["parallel_tool_calls"] = false,
            ["max_completion_tokens"] = 512
        }, cancellationToken, "tool_call");

        if (!toolMessage.TryGetProperty("tool_calls", out var calls)
            || calls.ValueKind != JsonValueKind.Array || calls.GetArrayLength() != 1)
            throw new InvalidOperationException("AI must request exactly one inventory facts tool call.");

        var call = calls[0];
        if (!call.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(id.GetString())
            || !call.TryGetProperty("type", out var type) || type.GetString() != "function"
            || !call.TryGetProperty("function", out var function)
            || function.ValueKind != JsonValueKind.Object
            || !function.TryGetProperty("name", out var name) || name.GetString() != tools[0].Name
            || !function.TryGetProperty("arguments", out var arguments)
            || arguments.ValueKind != JsonValueKind.String
            || arguments.GetString() is not { Length: > 0 and <= 4096 } argumentsJson)
            throw new InvalidOperationException("AI returned an unsupported inventory tool call.");

        using var parsedArguments = JsonDocument.Parse(argumentsJson);
        var toolResult = await executeTool(name.GetString()!, parsedArguments.RootElement, cancellationToken);
        _logger.LogInformation("Dashboard MCP tool {ToolName} completed; requesting final dashboard.", tools[0].Name);

        // Preserve the provider's tool-call ID so its result belongs to this turn.
        messages.Add(new { role = "assistant", content = (string?)null, tool_calls = calls });
        messages.Add(new { role = "tool", tool_call_id = id.GetString(), content = toolResult });
        // The full dashboard contract is only needed for final generation, not
        // the evidence-selection turn. Omit tool schemas on this final turn.
        messages[0] = new { role = "system", content = instructions };

        var finalMessage = await SendFinalAsync(new Dictionary<string, object>
        {
            ["model"] = _settings.Model,
            ["messages"] = messages,
            ["max_completion_tokens"] = 3072,
            ["response_format"] = new { type = "json_object" }
        }, cancellationToken);

        if (finalMessage.TryGetProperty("tool_calls", out var extraCalls)
            && extraCalls.ValueKind == JsonValueKind.Array && extraCalls.GetArrayLength() > 0)
            throw new InvalidOperationException("AI exceeded the inventory tool-call limit.");

        return ReadContent(finalMessage);
    }

    private async Task<JsonElement> SendFinalAsync(Dictionary<string, object> payload, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync(payload, cancellationToken, "dashboard_json");
        }
        catch (AiRateLimitException exception) when (exception.RetryAfterSeconds <= 5)
        {
            _logger.LogInformation("Waiting {Seconds} seconds before one final-dashboard retry.", exception.RetryAfterSeconds + 1);
            await Task.Delay(TimeSpan.FromSeconds(exception.RetryAfterSeconds + 1), cancellationToken);
            // A second 429 propagates to the shared cooldown. Never loop retries.
            return await SendAsync(payload, cancellationToken, "dashboard_json");
        }
    }

    private async Task<JsonElement> SendAsync(
        Dictionary<string, object> payload,
        CancellationToken cancellationToken,
        string stage = "text_completion")
    {

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
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            string? providerMessage = null;
            string? providerCode = null;

            try
            {
                using var errorDocument = JsonDocument.Parse(errorBody);
                if (errorDocument.RootElement.ValueKind == JsonValueKind.Object &&
                    errorDocument.RootElement.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        providerMessage = message.GetString();
                    if (error.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                        providerCode = code.GetString();
                }
            }
            catch (JsonException)
            {
                // Preserve HTTP status even if the provider returns HTML or an empty body.
            }

            if (providerCode is not { Length: > 0 and <= 80 }
                || !providerCode.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'))
                providerCode = null;
            if (!string.IsNullOrEmpty(_settings.ApiKey))
                providerMessage = providerMessage?.Replace(_settings.ApiKey, "[redacted]", StringComparison.Ordinal);

            _logger.LogWarning(
                "Groq HTTP {Status} at {Stage} for model {Model}. Code: {Code}. Provider message: {ProviderMessage}",
                (int)response.StatusCode, stage, _settings.Model, providerCode,
                providerMessage is { Length: > 1500 } ? providerMessage[..1500] : providerMessage);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "Groq rate limit for model {Model}. Retry-After: {RetryAfter}. Provider message: {ProviderMessage}",
                    _settings.Model,
                    response.Headers.RetryAfter?.ToString(),
                    providerMessage is { Length: > 1500 } ? providerMessage[..1500] : providerMessage);

                var retryAfter = response.Headers.RetryAfter;
                var wait = retryAfter?.Delta ??
                    (retryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(60);
                throw new AiRateLimitException(
                    (int)Math.Clamp(Math.Ceiling(wait.TotalSeconds), 1, int.MaxValue));
            }

            throw new AiProviderException(response.StatusCode, stage, providerCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0 ||
            choices[0].ValueKind != JsonValueKind.Object ||
            !choices[0].TryGetProperty("message", out var responseMessage) ||
            responseMessage.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Groq returned no response choices.");
        }

        if (choices[0].TryGetProperty("finish_reason", out var finishReason)
            && finishReason.ValueKind == JsonValueKind.String && finishReason.GetString() == "length")
            throw new InvalidOperationException("Groq response exceeded its output limit.");

        return responseMessage.Clone();
    }

    private static string ReadContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var responseContent)
            || responseContent.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Groq returned no text response.");

        string? content = responseContent.GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Groq returned an empty response.");
        }

        return content;
    }
}
