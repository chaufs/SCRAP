using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageDestructionAndCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateOfDestruction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestructionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganizationAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    SecurityStandard = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoftwareToolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SoftwareToolVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateOfDestruction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageDestructionRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    TeardownBatchId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageDestructionRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageDestructionRecord_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorageDestructionRecord_TeardownBatches_TeardownBatchId",
                        column: x => x.TeardownBatchId,
                        principalTable: "TeardownBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateOfDestructionItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateOfDestructionId = table.Column<int>(type: "int", nullable: false),
                    StorageDestructionRecordId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateOfDestructionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateOfDestructionItem_CertificateOfDestruction_CertificateOfDestructionId",
                        column: x => x.CertificateOfDestructionId,
                        principalTable: "CertificateOfDestruction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateOfDestruction_CertificateNumber",
                table: "CertificateOfDestruction",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateOfDestructionItem_CertificateOfDestructionId",
                table: "CertificateOfDestructionItem",
                column: "CertificateOfDestructionId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageDestructionRecord_InventoryId",
                table: "StorageDestructionRecord",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageDestructionRecord_TeardownBatchId",
                table: "StorageDestructionRecord",
                column: "TeardownBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateOfDestructionItem");

            migrationBuilder.DropTable(
                name: "StorageDestructionRecord");

            migrationBuilder.DropTable(
                name: "CertificateOfDestruction");
        }
    }
}
