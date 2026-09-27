using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class moreCompanyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Warehouse_Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "UOM_Category",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "UnitOfMeasure",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ProductUomConversion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "GoodReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "GoodReceiptItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Categories",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Warehouse_Locations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UOM_Category");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UnitOfMeasure");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ProductUomConversion");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "GoodReceipts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "GoodReceiptItems");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Categories");
        }
    }
}
