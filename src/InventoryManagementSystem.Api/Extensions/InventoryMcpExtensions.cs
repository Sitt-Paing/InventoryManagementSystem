using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InventoryManagementSystem.Api.Mcp;
using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore;

namespace InventoryManagementSystem.Api.Extensions;

public static class InventoryMcpExtensions
{
    public const string EndpointPath = "/api/process/mcp";

    public static IServiceCollection AddInventoryMcp(this IServiceCollection services)
    {
        services.AddHttpClient(InventoryMcpClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false
        });
        services.AddScoped<InventoryMcpClient>();
        services.AddScoped<IDashboardMcpClient>(provider => provider.GetRequiredService<InventoryMcpClient>());

        services.AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithTools<InventoryDashboardTools>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            });

        // MCP is called server-to-server with a Bearer token. Do not use ambient
        // browser cookies on this minimal API endpoint, which is outside MVC CSRF filters.
        // Keep the existing cookie handler for all other inventory API routes.
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var browserHandler = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                if (!context.Request.Path.StartsWithSegments(EndpointPath))
                    await browserHandler(context);
            };
        });

        return services;
    }

    public static IEndpointConventionBuilder MapInventoryMcp(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMcp(EndpointPath)
            .RequireAuthorization("InternalUser")
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                int.TryParse(context.User.FindFirst("company_id")?.Value, out var companyId)
                && companyId > 0))
            .WithDisplayName("Inventory MCP server");
    }
}
