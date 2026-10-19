using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseReturnNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "PurchaseReturnNumberSequence");

            // Continue above existing PR-yyyy-numeric numbers, including deleted documents.
            migrationBuilder.Sql(@"
                DECLARE @next bigint = (
                    SELECT ISNULL(MAX(TRY_CONVERT(bigint, SUBSTRING(ReturnNo, 9, 42))), 0) + 1
                    FROM PurchaseReturns
                    WHERE ReturnNo LIKE 'PR-[0-9][0-9][0-9][0-9]-%');
                DECLARE @statement nvarchar(200) =
                    N'ALTER SEQUENCE dbo.PurchaseReturnNumberSequence RESTART WITH ' + CONVERT(nvarchar(30), @next);
                EXEC sys.sp_executesql @statement;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "PurchaseReturnNumberSequence");
        }
    }
}
