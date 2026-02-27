using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSlas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CriticalResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HighResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    MediumResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    LowResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    InfoResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSlas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSlas_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSlas_CustomerId_Unique",
                table: "CustomerSlas",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSlas_IsActive",
                table: "CustomerSlas",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSlas");
        }
    }
}