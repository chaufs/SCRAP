using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyFinanceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyFinanceTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFinanceTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFinanceTransactions_SourceReference",
                table: "CompanyFinanceTransactions",
                column: "SourceReference",
                unique: true,
                filter: "[SourceReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFinanceTransactions_TransactionDate",
                table: "CompanyFinanceTransactions",
                column: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyFinanceTransactions");
        }
    }
}
