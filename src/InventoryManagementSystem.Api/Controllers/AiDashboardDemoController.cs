using System.Text.Json;
using InventoryManagementSystem.Api.Models;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Infrastructure.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/ai-demo/dashboard")]
public sealed class AiDashboardDemoController(GroqAiClient client, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly string[] LabelKeys =
    [
        "productCount", "riskCount", "openOrders", "stockTitle", "product",
        "available", "cover", "leadTime", "status", "days", "noHistory",
        "attentionTitle", "ordersTitle", "overdue", "awaiting", "snapshot"
    ];

    [HttpPost]
    public async Task<IActionResult> Generate(
        CancellationToken cancellationToken,
        [FromQuery] string language = "en")
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (language is not ("en" or "mm"))
            return BadRequest(new DefaultResponseModel { StatusCode = 400, Success = false, Message = "Language must be en or mm." });

        // Only fictional facts are sent to the model in this learning step.
        var products = new[]
        {
            new { Id = "DEMO-PAPER", Name = "A4 Paper", Unit = "reams", Stock = 40m, DailyUsage = (decimal?)10m, LeadDays = 7, CoverDays = (decimal?)4m, Risk = "RISK" },
            new { Id = "DEMO-GLOVES", Name = "Safety Gloves", Unit = "pairs", Stock = 72m, DailyUsage = (decimal?)6m, LeadDays = 4, CoverDays = (decimal?)12m, Risk = "SUFFICIENT" },
            new { Id = "DEMO-TONER", Name = "Printer Toner", Unit = "cartridges", Stock = 18m, DailyUsage = (decimal?)null, LeadDays = 5, CoverDays = (decimal?)null, Risk = "UNKNOWN" }
        };
        var facts = new
        {
            SnapshotDate = "2026-10-22",
            Products = products,
            Orders = new[]
            {
                new { Number = "DEMO-PO-001", Supplier = "Demo Paper Supply", Expected = "2026-10-20", Overdue = true },
                new { Number = "DEMO-PO-002", Supplier = "Demo Safety Supply", Expected = "2026-10-28", Overdue = false }
            }
        };

        object TextObjectSchema(string[] keys) => new
        {
            type = "object",
            properties = keys.ToDictionary(key => key, _ => new { type = "string" }),
            required = keys,
            additionalProperties = false
        };
        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "localized_inventory_dashboard",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        summary = new { type = "string" },
                        labels = TextObjectSchema(LabelKeys),
                        products = new { type = "array", items = TextObjectSchema(["id", "displayName", "unitLabel", "statusLabel", "explanation"]) }
                    },
                    required = new[] { "title", "summary", "labels", "products" },
                    additionalProperties = false
                }
            }
        };

        string outputLanguage = language == "mm" ? "Burmese (Myanmar), using Myanmar Unicode" : "English";
        string instructions = $"""
            Generate localized inventory dashboard display content as JSON.
            All display text must be in {outputLanguage}. IDs must remain unchanged.
            Use only supplied facts, which are fictional sample records, not live inventory.
            Produce a short title, a two-sentence summary, short UI labels, and exactly
            one product entry for each supplied ID. Translate generic product names,
            units and status labels naturally, retaining A4 and other model codes.
            Each explanation must be short and based on the supplied product risk.
            UNKNOWN means insufficient usage history, not zero demand. SUFFICIENT means
            coverage exceeds lead time, not that no purchase is ever needed.
            days is a unit label only, with no number or placeholder. noHistory is a
            short insufficient-data label. Never invent causes or claim orders were placed.
            Keep numbers unchanged. Supplier names and order numbers are proper names.
            """;

        try
        {
            string json = await client.GenerateAsync(
                instructions, JsonSerializer.Serialize(facts), cancellationToken, responseFormat);
            var content = JsonSerializer.Deserialize<AiDashboardCopy>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var ids = products.Select(product => product.Id).ToHashSet(StringComparer.Ordinal);
            if (content is null || string.IsNullOrWhiteSpace(content.Title) || string.IsNullOrWhiteSpace(content.Summary)
                || content.Labels is null || content.Labels.Count != LabelKeys.Length
                || LabelKeys.Any(key => !content.Labels.TryGetValue(key, out var label) || string.IsNullOrWhiteSpace(label))
                || content.Products is null || content.Products.Count != ids.Count
                || content.Products.Any(product => product is null || !ids.Contains(product.Id)
                    || new[] { product.DisplayName, product.UnitLabel, product.StatusLabel, product.Explanation }.Any(string.IsNullOrWhiteSpace))
                || content.Products.Select(product => product.Id).Distinct(StringComparer.Ordinal).Count() != ids.Count)
                throw new JsonException("AI dashboard content does not match the supplied records.");

            return Ok(new DefaultResponseModel
            {
                StatusCode = 200, Success = true,
                Data = new { Language = language, IsSampleData = true, Facts = facts, Content = content }
            });
        }
        catch (JsonException)
        {
            return Failure(502, "AI returned incomplete dashboard content. Please retry.");
        }
        catch (HttpRequestException exception)
        {
            return exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                ? Failure(429, "AI free-plan limit reached. Try again later.")
                : Failure(502, "Could not generate the AI dashboard.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(504, "AI dashboard request timed out.");
        }
        catch (InvalidOperationException)
        {
            return Failure(502, "AI returned no usable dashboard content.");
        }
    }

    private ObjectResult Failure(int status, string message) => StatusCode(status,
        new DefaultResponseModel { StatusCode = status, Success = false, Message = message });
}
