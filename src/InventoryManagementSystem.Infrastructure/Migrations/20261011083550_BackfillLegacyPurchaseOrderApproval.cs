using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyPurchaseOrderApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE po
                SET po.ApprovalStatus = 2
                FROM PurchaseOrders AS po
                WHERE po.ApprovalStatus = 0
                  AND po.DeletedOn IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM GoodReceipts AS gr
                      WHERE gr.PurchaseOrderId = po.Id
                        AND gr.DeletedOn IS NULL
                  );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
