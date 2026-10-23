using System.ComponentModel;
using System.Globalization;
using FluentValidation;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using InventoryManagementSystem.Application.Dashboard.Queries.GetDashboardFacts;
using MediatR;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace InventoryManagementSystem.Api.Mcp;

[McpServerToolType]
public sealed class InventoryDashboardTools(ISender sender)
{
    [McpServerTool(Name = "GetDashboardFacts", Title = "Inventory dashboard facts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Read current product stock, active product count, approved open purchase order count, " +
        "and posted departmental usage for a date range of up to 90 days. " +
        "Stock and open orders are current snapshots; only usage is date-filtered. " +
        "Stock issues are usage, not sales. Missing usage dates do not mean zero demand. " +
        "Company scope comes from the authenticated user, never from tool arguments.")]
    public async Task<DashboardFactsDto> GetDashboardFactsAsync(
        [Description("Inclusive start date in yyyy-MM-dd format.")] string startDate,
        [Description("Inclusive end date in yyyy-MM-dd format; no more than 90 days including the start date.")] string endDate,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var start) ||
            !DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var end))
        {
            throw new McpException("Both dates must use yyyy-MM-dd format.");
        }

        try
        {
            return await sender.Send(new GetDashboardFactsQuery(start, end), cancellationToken);
        }
        catch (ValidationException exception)
        {
            throw new McpException(string.Join(" ", exception.Errors
                .Select(error => error.ErrorMessage).Distinct()));
        }
    }
}
