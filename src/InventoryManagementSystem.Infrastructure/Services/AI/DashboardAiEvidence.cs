using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using InventoryManagementSystem.Application.Dashboard.DTOs;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

// Lossless model-only encoding: repeated product metadata is sent once.
// The MCP tool and browser response retain the original typed facts contract.
internal static class DashboardAiEvidence
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // Preserve Myanmar/product text rather than sending literal \uXXXX sequences
        // inside the tool-result string. This JSON is never embedded in HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(DashboardFactsDto facts)
    {
        var products = facts.ProductStocks.Select(product => new
            { product.ProductId, product.ProductName, BaseUnit = product.BaseUnitSymbol })
            .Concat(facts.DailyProductUsage.Select(usage => new
                { usage.ProductId, usage.ProductName, BaseUnit = usage.BaseUnitSymbol }))
            .DistinctBy(product => product.ProductId).ToArray();
        var indexes = products.Select((product, index) => (product.ProductId, index))
            .ToDictionary(product => product.ProductId, product => product.index);

        return JsonSerializer.Serialize(new
        {
            Encoding = "columnar-v1",
            facts.DataAsOfUtc,
            facts.SnapshotDate, facts.StartDate, facts.EndDate,
            facts.ActiveProductCount, facts.OpenPurchaseOrderCount, facts.StockHealth,
            // Actual GUIDs stay in the request's lookup; the model selects groups,
            // and the server expands those selections back to real product IDs.
            ProductColumns = new[] { "productName", "baseUnit" },
            Products = products.Select(product => new object[]
                { product.ProductName, product.BaseUnit }),
            StockColumns = new[] { "productIndex", "availableQuantity", "reorderLevel" },
            ProductStocks = facts.ProductStocks.Select(product => new object[]
                { indexes[product.ProductId], product.AvailableQuantity, product.ReorderLevel }),
            UsageColumns = new[] { "date", "productIndex", "issuedQuantity" },
            DailyProductUsage = facts.DailyProductUsage.Select(usage => new object[]
                { usage.Date, indexes[usage.ProductId], usage.IssuedQuantity }),
            ChartSelections = GetSelections(facts).Select(selection => new
            {
                key = selection.Key, dataset = selection.Value.Dataset,
                unit = products[indexes[selection.Value.ProductIds[0]]].BaseUnit,
                types = selection.Value.ProductsByType.Keys,
                products = selection.Value.ProductIds.Select(id => indexes[id])
            }),
            SummaryEvidence = new
            {
                AttentionSample = facts.ProductStocks.Where(product => product.AvailableQuantity <= 0
                        || product.AvailableQuantity <= product.ReorderLevel)
                    .OrderBy(product => product.AvailableQuantity).Take(6)
                    .Select(product => new { product.ProductName, BaseUnit = product.BaseUnitSymbol, product.AvailableQuantity, product.ReorderLevel }),
                RecentUsageSample = facts.DailyProductUsage.OrderByDescending(usage => usage.Date).Take(3)
                    .Select(usage => new { usage.Date, usage.ProductName, BaseUnit = usage.BaseUnitSymbol, usage.IssuedQuantity })
            }
        }, Options);
    }

    public static string ExpandSelections(string json, DashboardFactsDto facts, out int rejectedSelections)
    {
        rejectedSelections = 0;
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new JsonException("Expected a dashboard JSON object.");
        var selections = GetSelections(facts);
        if (root["widgets"] is JsonArray widgets)
        {
            foreach (var widget in widgets.OfType<JsonObject>().ToArray())
            {
                if (widget["productIds"] is not JsonArray { Count: 1 } ids
                    || ids[0] is not JsonValue value || !value.TryGetValue<string>(out var key)
                    || key is null || !key.StartsWith('@')) continue;

                if (!selections.TryGetValue(key, out var selection)
                    || ReadString(widget["datasetKey"]) != selection.Dataset
                    || ReadString(widget["type"]) is not { } type
                    || !selection.ProductsByType.TryGetValue(type, out var productIds))
                {
                    widgets.Remove(widget);
                    rejectedSelections++;
                    continue;
                }

                widget["productIds"] = new JsonArray(productIds
                    .Select(id => (JsonNode?)JsonValue.Create(id.ToString())).ToArray());
            }
        }
        return root.ToJsonString(Options);
    }

    private static string? ReadString(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private sealed record Selection(string Dataset, Guid[] ProductIds, Dictionary<string, Guid[]> ProductsByType);

    private static Dictionary<string, Selection> GetSelections(DashboardFactsDto facts)
    {
        var selections = new Dictionary<string, Selection>(StringComparer.Ordinal);
        var index = 0;
        foreach (var unit in facts.ProductStocks.Where(product => !string.IsNullOrWhiteSpace(product.BaseUnit))
            .GroupBy(product => product.BaseUnit.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var ids = unit.Select(product => product.ProductId).ToArray();
            var types = new Dictionary<string, Guid[]> { ["bar_chart"] = ids, ["table"] = ids };
            var shares = unit.Where(product => product.AvailableQuantity >= 0)
                .OrderByDescending(product => product.AvailableQuantity).Take(8).ToArray();
            if (shares.Length >= 2 && shares.Sum(product => product.AvailableQuantity) > 0)
            {
                types["pie_chart"] = shares.Select(product => product.ProductId).ToArray();
                types["doughnut_chart"] = types["pie_chart"];
            }
            selections[$"@stock-{index}"] = new Selection("productStocks", ids, types);
            index++;
        }

        index = 0;
        foreach (var unit in facts.DailyProductUsage.Where(usage => !string.IsNullOrWhiteSpace(usage.BaseUnit))
            .GroupBy(usage => usage.BaseUnit.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var products = unit.GroupBy(usage => usage.ProductId).ToArray();
            var ids = products.Select(product => product.Key).ToArray();
            var types = new Dictionary<string, Guid[]> { ["bar_chart"] = ids, ["stacked_bar_chart"] = ids, ["table"] = ids };
            var trends = products.Where(product => product.Select(usage => usage.Date).Distinct().Count() >= 2)
                .Select(product => product.Key).ToArray();
            if (trends.Length > 0)
            {
                types["line_chart"] = trends;
                types["area_chart"] = trends;
            }
            var shares = products.Select(product => new { Id = product.Key, Quantity = product.Sum(usage => usage.IssuedQuantity) })
                .Where(product => product.Quantity >= 0).OrderByDescending(product => product.Quantity).Take(8).ToArray();
            if (shares.Length >= 2 && shares.Sum(product => product.Quantity) > 0)
            {
                types["pie_chart"] = shares.Select(product => product.Id).ToArray();
                types["doughnut_chart"] = types["pie_chart"];
            }
            selections[$"@usage-{index}"] = new Selection("dailyProductUsage", ids, types);
            index++;
        }
        return selections;
    }
}
