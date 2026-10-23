using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Dashboard.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace InventoryManagementSystem.Api.Mcp;

public sealed class InventoryMcpClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IHostEnvironment environment) : IDashboardMcpClient
{
    public const string HttpClientName = "InventoryMcp";

    public Task<IReadOnlyList<DashboardToolDefinitionDto>> GetToolsAsync(CancellationToken cancellationToken)
        => WithClientAsync<IReadOnlyList<DashboardToolDefinitionDto>>(async client =>
        {
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            return tools.Where(tool => tool.Name == "GetDashboardFacts")
                .Select(tool => new DashboardToolDefinitionDto(
                    tool.Name, tool.Description ?? string.Empty, tool.JsonSchema.Clone()))
                .ToArray();
        }, cancellationToken);

    public Task<DashboardFactsDto> GetFactsAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
        => WithClientAsync(async client =>
        {
            var result = await client.CallToolAsync("GetDashboardFacts",
                new Dictionary<string, object?>
                {
                    ["startDate"] = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["endDate"] = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                }, cancellationToken: cancellationToken);

            if (result.IsError == true)
                throw new McpException("The inventory facts tool could not complete the request.");

            return JsonSerializer.Deserialize<DashboardFactsDto>(
                JsonSerializer.Serialize(result.StructuredContent),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new McpException("The inventory facts tool returned no structured data.");
        }, cancellationToken);

    private async Task<T> WithClientAsync<T>(Func<McpClient, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await WithClientCoreAsync(action, cancellationToken);
        }
        catch (McpException exception)
        {
            throw new DashboardToolException("The inventory MCP tool request failed.", exception);
        }
    }

    private async Task<T> WithClientCoreAsync<T>(Func<McpClient, Task<T>> action, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("MCP requires an authenticated HTTP request.");
        var authentication = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!authentication.Succeeded)
            throw new UnauthorizedAccessException("MCP requires an authenticated user.");

        // SaveToken is enabled on the existing JWT handler. Use its validated ticket,
        // rather than reading an unvalidated cookie/header or creating a service identity.
        var accessToken = authentication.Properties?.GetTokenValue("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new UnauthorizedAccessException("The authenticated access token is unavailable.");

        var endpointValue = configuration["Ai:Mcp:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpointValue) && environment.IsDevelopment())
            endpointValue = "https://localhost:7152/api/process/mcp";

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new InvalidOperationException("Configure Ai:Mcp:Endpoint with the inventory MCP HTTPS URL.");

        // The destination is trusted deployment configuration, never a request Host header
        // or a model argument. Each operation gets its own credentials and connection.
        using var http = httpClientFactory.CreateClient(HttpClientName);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await using var client = await McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions { Endpoint = endpoint }, http),
            cancellationToken: cancellationToken);

        return await action(client);
    }
}
