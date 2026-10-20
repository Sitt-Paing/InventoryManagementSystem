using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkStockIssueMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockIssueId",
                table: "StockTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StockIssueItemId",
                table: "StockTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockIssueId",
                table: "StockTransactions",
                column: "StockIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockIssueItemId",
                table: "StockTransactions",
                column: "StockIssueItemId",
                unique: true,
                filter: "[StockIssueItemId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_StockIssueItems_StockIssueItemId",
                table: "StockTransactions",
                column: "StockIssueItemId",
                principalTable: "StockIssueItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_StockIssues_StockIssueId",
                table: "StockTransactions",
                column: "StockIssueId",
                principalTable: "StockIssues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_StockIssueItems_StockIssueItemId",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_StockIssues_StockIssueId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_StockIssueId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_StockIssueItemId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "StockIssueId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "StockIssueItemId",
                table: "StockTransactions");
        }
    }
}
