using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillReceiptAndReturnAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM GoodReceiptItems i
                    LEFT JOIN PurchaseOrderItems p ON p.Id = i.PurchaseOrderItemId
                    LEFT JOIN GoodReceipts r ON r.Id = i.GoodReceiptId
                    WHERE p.Id IS NULL OR r.Id IS NULL
                        OR p.ProductId <> i.ProductId OR p.UomId <> i.UomId
                        OR p.PurchaseOrderId <> r.PurchaseOrderId
                        OR ISNULL(p.CompanyId, -1) <> ISNULL(i.CompanyId, -1)
                        OR ISNULL(r.CompanyId, -1) <> ISNULL(i.CompanyId, -1)
                )
                    THROW 51010, 'Receipt amounts cannot be backfilled: a source PO item does not match.', 1;

                IF EXISTS (
                    SELECT 1 FROM PurchaseReturnItems i
                    LEFT JOIN GoodReceiptItems g ON g.Id = i.GoodReceiptItemId
                    LEFT JOIN PurchaseReturns r ON r.Id = i.PurchaseReturnId
                    WHERE g.Id IS NULL OR r.Id IS NULL
                        OR ISNULL(g.CompanyId, -1) <> ISNULL(i.CompanyId, -1)
                        OR ISNULL(r.CompanyId, -1) <> ISNULL(i.CompanyId, -1)
                )
                    THROW 51011, 'Return amounts cannot be backfilled: a source GRN item does not match.', 1;

                -- Preserve amounts already recorded by the new handlers.
                UPDATE i SET UnitPrice = p.UnitPrice
                FROM GoodReceiptItems i
                INNER JOIN PurchaseOrderItems p ON p.Id = i.PurchaseOrderItemId
                WHERE i.UnitPrice = 0 AND i.LineAmount = 0;

                UPDATE GoodReceiptItems
                SET LineAmount = ROUND(ReceivedQuantity * UnitPrice, 2);

                UPDATE r SET TotalAmount = COALESCE(amounts.Total, 0)
                FROM GoodReceipts r
                OUTER APPLY (
                    SELECT SUM(i.LineAmount) AS Total FROM GoodReceiptItems i
                    WHERE i.GoodReceiptId = r.Id AND i.DeletedOn IS NULL
                ) amounts;

                UPDATE i SET UnitPrice = g.UnitPrice
                FROM PurchaseReturnItems i
                INNER JOIN GoodReceiptItems g ON g.Id = i.GoodReceiptItemId
                WHERE i.UnitPrice = 0 AND i.LineAmount = 0;

                UPDATE PurchaseReturnItems
                SET LineAmount = ROUND(ReturnQuantity * UnitPrice, 2);

                UPDATE r SET TotalAmount = COALESCE(amounts.Total, 0)
                FROM PurchaseReturns r
                OUTER APPLY (
                    SELECT SUM(i.LineAmount) AS Total FROM PurchaseReturnItems i
                    WHERE i.PurchaseReturnId = r.Id AND i.DeletedOn IS NULL
                ) amounts;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep reconstructed historical values; the preceding schema migration removes the new columns.
        }
    }
}
