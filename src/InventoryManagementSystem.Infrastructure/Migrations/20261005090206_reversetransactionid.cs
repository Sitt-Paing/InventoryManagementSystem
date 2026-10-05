using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class reversetransactionid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReversesStockTransactionId",
                table: "StockTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ReversesStockTransactionId",
                table: "StockTransactions",
                column: "ReversesStockTransactionId",
                unique: true,
                filter: "[ReversesStockTransactionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_StockTransactions_ReversesStockTransactionId",
                table: "StockTransactions",
                column: "ReversesStockTransactionId",
                principalTable: "StockTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_StockTransactions_ReversesStockTransactionId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_ReversesStockTransactionId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "ReversesStockTransactionId",
                table: "StockTransactions");
        }
    }
}
