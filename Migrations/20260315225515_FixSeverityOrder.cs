using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class FixSeverityOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old: Low=0, Medium=1, High=2, Critical=3, Info=4
            // New: Info=0, Low=1, Medium=2, High=3, Critical=4
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 99 WHERE Severity = 4;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 4 WHERE Severity = 3;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 3 WHERE Severity = 2;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 2 WHERE Severity = 1;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 1 WHERE Severity = 0;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 0 WHERE Severity = 99;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 99 WHERE Severity = 0;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 0 WHERE Severity = 1;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 1 WHERE Severity = 2;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 2 WHERE Severity = 3;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 3 WHERE Severity = 4;");
            migrationBuilder.Sql("UPDATE Tickets SET Severity = 4 WHERE Severity = 99;");
        }
    }
}