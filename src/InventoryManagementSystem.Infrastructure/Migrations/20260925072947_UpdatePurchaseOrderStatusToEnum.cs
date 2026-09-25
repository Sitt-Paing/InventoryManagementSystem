using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseOrderStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.Sql(@"
                UPDATE po
                SET po.Status = CASE 
                    WHEN NOT EXISTS (SELECT 1 FROM PurchaseOrderItems poi WHERE poi.PurchaseOrderId = po.Id AND poi.DeletedOn IS NULL AND poi.ReceivedQuantity < poi.Quantity)
                         AND EXISTS (SELECT 1 FROM PurchaseOrderItems poi WHERE poi.PurchaseOrderId = po.Id AND poi.DeletedOn IS NULL)
                    THEN 2
                    WHEN EXISTS (SELECT 1 FROM PurchaseOrderItems poi WHERE poi.PurchaseOrderId = po.Id AND poi.DeletedOn IS NULL AND poi.ReceivedQuantity > 0)
                    THEN 1
                    ELSE 0
                END
                FROM PurchaseOrders po;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
