using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Clear transactional tables that will gain a non-nullable FK ──
            migrationBuilder.Sql("DELETE FROM [TeardownYields]");
            migrationBuilder.Sql("DELETE FROM [StorageDestructionRecord]");
            migrationBuilder.Sql("DELETE FROM [TeardownBatches]");
            migrationBuilder.Sql("DELETE FROM [Inventories]");
            migrationBuilder.Sql("DELETE FROM [CommoditySales]");
            migrationBuilder.Sql("DELETE FROM [CompanyFinanceTransactions]");

            // ── 2. Create Branches table ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id      = table.Column<int>(type: "int", nullable: false)
                                  .Annotation("SqlServer:Identity", "1, 1"),
                    Name    = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code    = table.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);

            // ── 3. BranchId FK on Users (nullable — Admin has no branch) ──────
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── 4. BranchId FK on Inventories (non-nullable) ──────────────────
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_BranchId",
                table: "Inventories",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── 5. BranchId FK on TeardownBatches (non-nullable) ──────────────
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "TeardownBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TeardownBatches_BranchId",
                table: "TeardownBatches",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeardownBatches_Branches_BranchId",
                table: "TeardownBatches",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── 6. BranchId FK on CommoditySales (non-nullable) ───────────────
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CommoditySales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CommoditySales_BranchId",
                table: "CommoditySales",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommoditySales_Branches_BranchId",
                table: "CommoditySales",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── 7. BranchId FK on CompanyFinanceTransactions (non-nullable) ───
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CompanyFinanceTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFinanceTransactions_BranchId",
                table: "CompanyFinanceTransactions",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyFinanceTransactions_Branches_BranchId",
                table: "CompanyFinanceTransactions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Users_Branches_BranchId",                    table: "Users");
            migrationBuilder.DropForeignKey(name: "FK_Inventories_Branches_BranchId",             table: "Inventories");
            migrationBuilder.DropForeignKey(name: "FK_TeardownBatches_Branches_BranchId",         table: "TeardownBatches");
            migrationBuilder.DropForeignKey(name: "FK_CommoditySales_Branches_BranchId",          table: "CommoditySales");
            migrationBuilder.DropForeignKey(name: "FK_CompanyFinanceTransactions_Branches_BranchId", table: "CompanyFinanceTransactions");

            migrationBuilder.DropIndex(name: "IX_Users_BranchId",                    table: "Users");
            migrationBuilder.DropIndex(name: "IX_Inventories_BranchId",             table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_TeardownBatches_BranchId",         table: "TeardownBatches");
            migrationBuilder.DropIndex(name: "IX_CommoditySales_BranchId",          table: "CommoditySales");
            migrationBuilder.DropIndex(name: "IX_CompanyFinanceTransactions_BranchId", table: "CompanyFinanceTransactions");

            migrationBuilder.DropColumn(name: "BranchId", table: "Users");
            migrationBuilder.DropColumn(name: "BranchId", table: "Inventories");
            migrationBuilder.DropColumn(name: "BranchId", table: "TeardownBatches");
            migrationBuilder.DropColumn(name: "BranchId", table: "CommoditySales");
            migrationBuilder.DropColumn(name: "BranchId", table: "CompanyFinanceTransactions");

            migrationBuilder.DropTable(name: "Branches");
        }
    }
}
