using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GoodReceiptFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GoodReceiptId",
                table: "StockTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GoodReceiptItemId",
                table: "StockTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_GoodReceiptId",
                table: "StockTransactions",
                column: "GoodReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_GoodReceiptItemId",
                table: "StockTransactions",
                column: "GoodReceiptItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_GoodReceiptItems_GoodReceiptItemId",
                table: "StockTransactions",
                column: "GoodReceiptItemId",
                principalTable: "GoodReceiptItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_GoodReceipts_GoodReceiptId",
                table: "StockTransactions",
                column: "GoodReceiptId",
                principalTable: "GoodReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_GoodReceiptItems_GoodReceiptItemId",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_GoodReceipts_GoodReceiptId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_GoodReceiptId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_GoodReceiptItemId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "GoodReceiptId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "GoodReceiptItemId",
                table: "StockTransactions");
        }
    }
}
