using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommoditySale",
                table: "CommoditySale");

            migrationBuilder.RenameTable(
                name: "CommoditySale",
                newName: "CommoditySales");

            migrationBuilder.RenameIndex(
                name: "IX_CommoditySale_InvoiceNumber",
                table: "CommoditySales",
                newName: "IX_CommoditySales_InvoiceNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommoditySales",
                table: "CommoditySales",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommoditySales",
                table: "CommoditySales");

            migrationBuilder.RenameTable(
                name: "CommoditySales",
                newName: "CommoditySale");

            migrationBuilder.RenameIndex(
                name: "IX_CommoditySales_InvoiceNumber",
                table: "CommoditySale",
                newName: "IX_CommoditySale_InvoiceNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommoditySale",
                table: "CommoditySale",
                column: "Id");
        }
    }
}
