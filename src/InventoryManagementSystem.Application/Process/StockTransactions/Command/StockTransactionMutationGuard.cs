using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command;

internal static class StockTransactionMutationGuard
{
    public static async Task EnsureCanModifyAsync(IApplicationDbContext context, StockTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.GoodReceiptId.HasValue || transaction.GoodReceiptItemId.HasValue
            || transaction.ReversesStockTransactionId.HasValue)
        {
            throw new InvalidOperationException("Receipt stock movements and their reversals must be managed through the goods receipt.");
        }

        if (await context.StockTransactions.AnyAsync(x => x.ReversesStockTransactionId == transaction.Id, cancellationToken))
        {
            throw new InvalidOperationException("A reversed stock movement cannot be edited or deleted.");
        }

        // Adjustments store an absolute balance, without the previous balance needed to undo them.
        if (string.Equals(transaction.TransactionType.Trim(), "ADJUSTMENT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("An existing stock adjustment cannot be edited or deleted. Create a new adjustment to correct the balance.");
        }
    }
}
