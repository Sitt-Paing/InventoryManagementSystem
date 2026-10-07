using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command;

internal static class StockTransactionWarehouseValidation
{
    public static async Task ValidateAsync(
        IApplicationDbContext context,
        int warehouseId,
        int warehouseLocationId,
        bool isTransfer,
        int? toWarehouseId,
        int? toWarehouseLocationId,
        CancellationToken cancellationToken)
    {
        await ValidateLocationAsync(context, warehouseId, warehouseLocationId, "Source", cancellationToken);

        if (!isTransfer)
        {
            return;
        }

        if (!toWarehouseId.HasValue || toWarehouseId.Value <= 0)
        {
            throw new InvalidOperationException("Destination Warehouse is required for stock transfer.");
        }

        if (warehouseId == toWarehouseId.Value)
        {
            throw new InvalidOperationException("Source warehouse and Destination warehouse cannot be the same.");
        }

        if (!toWarehouseLocationId.HasValue || toWarehouseLocationId.Value <= 0)
        {
            throw new InvalidOperationException("Destination Warehouse Location is required for stock transfer.");
        }

        await ValidateLocationAsync(context, toWarehouseId.Value, toWarehouseLocationId.Value, "Destination", cancellationToken);
    }

    private static async Task ValidateLocationAsync(
        IApplicationDbContext context,
        int warehouseId,
        int locationId,
        string role,
        CancellationToken cancellationToken)
    {
        var warehouseExists = await context.Warehouses
            .AnyAsync(x => x.Id == warehouseId && !x.DeletedOn.HasValue, cancellationToken);

        if (!warehouseExists)
        {
            throw new InvalidOperationException($"{role} warehouse was not found or has been deleted.");
        }

        var locationMatches = await context.WarehouseLocations
            .AnyAsync(x => x.Id == locationId && x.WarehouseId == warehouseId && !x.DeletedOn.HasValue, cancellationToken);

        if (!locationMatches)
        {
            throw new InvalidOperationException($"{role} location was not found, has been deleted, or does not belong to the selected warehouse.");
        }
    }
}
