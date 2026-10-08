using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM PurchaseOrders WHERE CompanyId IS NOT NULL
                    GROUP BY CompanyId, PurchaseOrderNo HAVING COUNT(*) > 1)
                    THROW 51002, 'Duplicate company PO numbers must be reconciled before this migration.', 1;");
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "PurchaseOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreateRequestHash",
                table: "PurchaseOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CompanyId_PurchaseOrderNo",
                table: "PurchaseOrders",
                columns: new[] { "CompanyId", "PurchaseOrderNo" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CompanyId_PurchaseOrderNo",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreateRequestHash",
                table: "PurchaseOrders");
        }
    }
}
