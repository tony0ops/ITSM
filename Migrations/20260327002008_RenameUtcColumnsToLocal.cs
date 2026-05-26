using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class RenameUtcColumnsToLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "UserProfiles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SlaDueUtc",
                table: "Tickets",
                newName: "SlaDueAt");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Tickets",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ClosedUtc",
                table: "Tickets",
                newName: "ClosedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "TicketComments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Teams",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "CustomerUsers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "CustomerSlas",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "UserProfiles",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "SlaDueAt",
                table: "Tickets",
                newName: "SlaDueUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Tickets",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                table: "Tickets",
                newName: "ClosedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TicketComments",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Teams",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CustomerUsers",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CustomerSlas",
                newName: "CreatedUtc");
        }
    }
}
