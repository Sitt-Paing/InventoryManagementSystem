using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkProductsToSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSuppliers",
                columns: table => new
                {
                    ProductId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSuppliers", x => new { x.ProductId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_SupplierId",
                table: "ProductSuppliers",
                column: "SupplierId");

            migrationBuilder.Sql("""
                INSERT INTO ProductSuppliers (ProductId, SupplierId)
                SELECT DISTINCT p.Id, s.Id
                FROM Products p
                INNER JOIN PurchaseOrderItems i ON i.ProductId = p.Id AND i.CompanyId = p.CompanyId
                INNER JOIN PurchaseOrders o ON o.Id = i.PurchaseOrderId AND o.CompanyId = p.CompanyId
                INNER JOIN Suppliers s ON s.Id = o.SupplierId AND s.CompanyId = p.CompanyId
                WHERE p.DeletedOn IS NULL AND s.DeletedOn IS NULL
                    AND i.DeletedOn IS NULL AND o.DeletedOn IS NULL
                    AND o.Status <> 3; -- Exclude cancelled purchase orders.
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSuppliers");
        }
    }
}
