using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Data;

#nullable disable

namespace WebApplication1.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008000000_AddTicketAttachments")]
public partial class AddTicketAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketAttachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                StoredFileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                UploadedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                UploadedByName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                UploadedByRole = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                IsCustomerVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketAttachments", x => x.Id);
                table.ForeignKey("FK_TicketAttachments_Tickets_TicketId", x => x.TicketId,
                    "Tickets", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_TicketAttachments_TicketId", "TicketAttachments", "TicketId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TicketAttachments");
    }
}
