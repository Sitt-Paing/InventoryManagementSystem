using FluentValidation;
using InventoryManagementSystem.Api.Mcp;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Exceptions;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Dashboard.Exceptions;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using InventoryManagementSystem.Application.Dashboard.Queries.GetDashboardFacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol;
using System.Text.Json;

namespace InventoryManagementSystem.Api.Controllers.Process;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/process/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardAiService _aiService;
    private readonly InventoryMcpClient _mcpClient;
    private readonly IValidator<GetDashboardFactsQuery> _factsValidator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardAiService aiService,
        InventoryMcpClient mcpClient,
        IValidator<GetDashboardFactsQuery> factsValidator,
        ILogger<DashboardController> logger)
    {
        _aiService = aiService;
        _mcpClient = mcpClient;
        _factsValidator = factsValidator;
        _logger = logger;
    }

    [HttpGet("facts")]
    [EndpointSummary("Get current inventory facts and posted usage for the selected period")]
    public async Task<IActionResult> GetFacts(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new GetDashboardFactsQuery(startDate, endDate),
            cancellationToken);

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Dashboard facts retrieved successfully.",
            Data = result
        });
    }

    [HttpPost("generate")]
    [EndpointSummary("Generate an AI dashboard definition from current inventory facts")]
    public async Task<IActionResult> Generate(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string language,
        CancellationToken cancellationToken)
    {
        if (language is not ("en" or "mm"))
        {
            return AiFailure(
                StatusCodes.Status400BadRequest,
                "Language must be 'en' or 'mm'.");
        }

        // Preserve the existing client-facing validation errors before making
        // the MCP HTTP call. The tool also validates requests independently.
        await _factsValidator.ValidateAndThrowAsync(
            new GetDashboardFactsQuery(startDate, endDate), cancellationToken);

        DashboardFactsDto facts;
        try
        {
            facts = await _mcpClient.GetFactsAsync(startDate, endDate, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return AiFailure(StatusCodes.Status401Unauthorized, "Please sign in again.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Inventory MCP connection failed.");
            var status = exception.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => StatusCodes.Status401Unauthorized,
                System.Net.HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };
            return AiFailure(status, status == StatusCodes.Status401Unauthorized
                ? "Please sign in again."
                : "Could not retrieve inventory facts through MCP.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AiFailure(StatusCodes.Status504GatewayTimeout, "Inventory MCP request timed out.");
        }
        catch (Exception exception) when (exception is DashboardToolException or McpException or JsonException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Inventory MCP facts request failed.");
            return AiFailure(StatusCodes.Status502BadGateway, "Could not retrieve inventory facts through MCP.");
        }

        try
        {
            var definition = await _aiService.GenerateAsync(facts, language, cancellationToken);

            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Dashboard generated successfully.",
                Data = new { Facts = facts, Definition = definition }
            });
        }
        catch (DashboardCoordinationException exception)
        {
            _logger.LogWarning(exception, "Shared dashboard generation coordination is unavailable.");
            return AiFailure(StatusCodes.Status503ServiceUnavailable,
                "AI generation is temporarily unavailable. Current inventory facts are still available.");
        }
        catch (DashboardToolException exception)
        {
            _logger.LogWarning(exception, "Dashboard evidence tool request failed.");
            return AiFailure(StatusCodes.Status502BadGateway, "Could not retrieve inventory facts through MCP.");
        }
        catch (DashboardGenerationException exception)
        {
            _logger.LogWarning(exception, "AI dashboard output failed validation for {Language}.", language);
            return AiFailure(
                StatusCodes.Status502BadGateway,
                "AI returned an unusable dashboard definition.");
        }
        catch (AiRateLimitException exception)
        {
            Response.Headers.RetryAfter = exception.RetryAfterSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status429TooManyRequests, new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
                Success = false,
                Message = "AI usage limit reached. Please try again later.",
                Data = new { RetryAfterSeconds = exception.RetryAfterSeconds }
            });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "AI dashboard provider request failed.");

            if (exception is AiProviderException providerException)
            {
                var message = providerException.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                        "AI provider credentials or permissions are invalid.",
                    System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.UnprocessableEntity =>
                        "AI provider rejected the dashboard request.",
                    System.Net.HttpStatusCode.NotFound => "The configured AI model or endpoint is unavailable.",
                    System.Net.HttpStatusCode.RequestEntityTooLarge =>
                        "The dashboard request exceeds the AI provider token limit. Select a shorter date range.",
                    _ => "AI provider is temporarily unavailable. Please try again later."
                };
                return StatusCode(StatusCodes.Status502BadGateway, new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status502BadGateway,
                    Success = false,
                    Message = message,
                    Data = new { ProviderStatusCode = (int?)providerException.StatusCode,
                        ProviderErrorCode = providerException.ErrorCode, Stage = providerException.Stage }
                });
            }

            return exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                ? AiFailure(StatusCodes.Status429TooManyRequests, "AI usage limit reached. Please try again later.")
                : AiFailure(StatusCodes.Status502BadGateway, "Could not contact the AI provider.");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "AI dashboard provider request timed out.");
            return AiFailure(StatusCodes.Status504GatewayTimeout, "AI request timed out. Please try again.");
        }
    }

    private ObjectResult AiFailure(int statusCode, string message)
    {
        return StatusCode(statusCode, new DefaultResponseModel
        {
            StatusCode = statusCode,
            Success = false,
            Message = message,
            Data = null
        });
    }
}
