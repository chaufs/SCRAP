using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCRAP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncEmployeeNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employee",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Employee",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Employee",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Preserve existing employee names before removing the legacy FullName column.
            migrationBuilder.Sql(@"
                UPDATE e
                SET
                    FirstName = CASE
                        WHEN CHARINDEX(' ', LTRIM(RTRIM(e.FullName))) > 0
                            THEN LEFT(LTRIM(RTRIM(e.FullName)), CHARINDEX(' ', LTRIM(RTRIM(e.FullName))) - 1)
                        ELSE LTRIM(RTRIM(e.FullName))
                    END,
                    LastName = CASE
                        WHEN CHARINDEX(' ', LTRIM(RTRIM(e.FullName))) > 0
                            THEN RIGHT(LTRIM(RTRIM(e.FullName)), CHARINDEX(' ', REVERSE(LTRIM(RTRIM(e.FullName)))) - 1)
                        ELSE LTRIM(RTRIM(e.FullName))
                    END,
                    MiddleName = NULL
                FROM Employee e;");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Employee");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Employee",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
