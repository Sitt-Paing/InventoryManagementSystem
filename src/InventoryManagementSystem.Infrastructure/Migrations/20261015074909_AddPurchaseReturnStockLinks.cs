using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseReturnStockLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseReturnId",
                table: "StockTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PurchaseReturnItemId",
                table: "StockTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseReturnId",
                table: "StockTransactions",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseReturnItemId",
                table: "StockTransactions",
                column: "PurchaseReturnItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_PurchaseReturnItems_PurchaseReturnItemId",
                table: "StockTransactions",
                column: "PurchaseReturnItemId",
                principalTable: "PurchaseReturnItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_PurchaseReturns_PurchaseReturnId",
                table: "StockTransactions",
                column: "PurchaseReturnId",
                principalTable: "PurchaseReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_PurchaseReturnItems_PurchaseReturnItemId",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_PurchaseReturns_PurchaseReturnId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_PurchaseReturnId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_PurchaseReturnItemId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseReturnId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseReturnItemId",
                table: "StockTransactions");
        }
    }
}
