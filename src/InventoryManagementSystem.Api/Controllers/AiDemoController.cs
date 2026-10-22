using System.Text.Json;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Infrastructure.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/ai-demo")]
public sealed class AiDemoController : ControllerBase
{
    private readonly GroqAiClient _client;
    private readonly IWebHostEnvironment _environment;

    public AiDemoController(
        GroqAiClient client,
        IWebHostEnvironment environment)
    {
        _client = client;
        _environment = environment;
    }

    [HttpPost("summary")]
    public async Task<IActionResult> GenerateSummary(
        CancellationToken cancellationToken,
        [FromQuery] string language = "en")
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (language is not ("en" or "mm"))
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = 400,
                Success = false,
                Message = "Language must be en or mm."
            });
        }

        var sampleData = new
        {
            ProductName = "A4 Paper",
            AvailableQuantity = 40,
            Unit = "ream",
            StockCoverDays = 4,
            SupplierLeadTimeDays = 7,
            PendingPurchaseQuantity = 0
        };

        string outputLanguage = language == "mm" ? "Burmese (Myanmar), using Myanmar Unicode" : "English";
        string instructions = $"""
            You write inventory dashboard summaries. Reply only in {outputLanguage}.
            Use only the supplied sample data. Keep product names and numbers unchanged.
            Supplier lead time means time from ordering to receipt.
            Pending purchase quantity means ordered goods not yet received, not payment.
            Do not invent causes or claim an order has been placed.
            Write two or three short sentences.
            """;

        string prompt = $"""
            Summarize the stock risk in this sample data:
            {JsonSerializer.Serialize(sampleData)}
            """;

        try
        {
            string summary = await _client.GenerateAsync(
                instructions,
                prompt,
                cancellationToken);

            return Ok(new DefaultResponseModel
            {
                StatusCode = 200,
                Success = true,
                Data = new
                {
                    IsSampleData = true,
                    Language = language,
                    Summary = summary
                }
            });
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(502, new DefaultResponseModel
            {
                StatusCode = 502,
                Success = false,
                Message = exception.StatusCode.HasValue
                    ? $"Groq returned HTTP {(int)exception.StatusCode.Value}."
                    : "Could not connect to Groq."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new DefaultResponseModel
            {
                StatusCode = 504,
                Success = false,
                Message = "Groq request timed out."
            });
        }
    }
}
