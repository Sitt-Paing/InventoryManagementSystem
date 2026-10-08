using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "PurchaseOrderNumberSequence");
            // Start above existing numeric PO suffixes, including deleted documents.
            migrationBuilder.Sql(@"
                DECLARE @next bigint = (
                    SELECT ISNULL(MAX(TRY_CONVERT(bigint, SUBSTRING(PurchaseOrderNo, 9, 42))), 0) + 1
                    FROM PurchaseOrders
                    WHERE PurchaseOrderNo LIKE 'PO-[0-9][0-9][0-9][0-9]-%');
                DECLARE @statement nvarchar(200) =
                    N'ALTER SEQUENCE dbo.PurchaseOrderNumberSequence RESTART WITH ' + CONVERT(nvarchar(30), @next);
                EXEC sys.sp_executesql @statement;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "PurchaseOrderNumberSequence");
        }
    }
}
