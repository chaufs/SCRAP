using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceProcurementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchCode",
                table: "Inventories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcurementQuantity",
                table: "Inventories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseCost",
                table: "Inventories",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Inventories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchasedFrom",
                table: "Inventories",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_BatchCode",
                table: "Inventories",
                column: "BatchCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_BatchCode",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "BatchCode",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ProcurementQuantity",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "PurchaseCost",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "PurchasedFrom",
                table: "Inventories");
        }
    }
}
