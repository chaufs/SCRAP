using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeardownAndRawInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchetypeRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceCategoryId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeightKgPerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeRecipes_DeviceCategories_DeviceCategoryId",
                        column: x => x.DeviceCategoryId,
                        principalTable: "DeviceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RawInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentTotalWeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawInventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeardownBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessedByUserId = table.Column<int>(type: "int", nullable: false),
                    DeviceCategoryId = table.Column<int>(type: "int", nullable: false),
                    QuantityDismantled = table.Column<int>(type: "int", nullable: false),
                    DateProcessed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeardownBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeardownBatches_DeviceCategories_DeviceCategoryId",
                        column: x => x.DeviceCategoryId,
                        principalTable: "DeviceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeardownYields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeardownBatchId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeardownYields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeardownYields_TeardownBatches_TeardownBatchId",
                        column: x => x.TeardownBatchId,
                        principalTable: "TeardownBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeRecipes_DeviceCategoryId",
                table: "ArchetypeRecipes",
                column: "DeviceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TeardownBatches_DeviceCategoryId",
                table: "TeardownBatches",
                column: "DeviceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TeardownYields_TeardownBatchId",
                table: "TeardownYields",
                column: "TeardownBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchetypeRecipes");

            migrationBuilder.DropTable(
                name: "RawInventories");

            migrationBuilder.DropTable(
                name: "TeardownYields");

            migrationBuilder.DropTable(
                name: "TeardownBatches");
        }
    }
}
