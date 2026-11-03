using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Data;

#nullable disable

namespace WebApplication1.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008195000_LinkAttachmentsToComments")]
public partial class LinkAttachmentsToComments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQLite cannot add a foreign key to an existing table directly, so rebuild
        // the small metadata table while copying its rows and file references intact.
        migrationBuilder.CreateTable(
            name: "__TicketAttachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                TicketCommentId = table.Column<int>(type: "INTEGER", nullable: true),
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
                table.PrimaryKey("PK___TicketAttachments", x => x.Id);
                table.ForeignKey("FK_TicketAttachments_Tickets_TicketId", x => x.TicketId,
                    "Tickets", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TicketAttachments_TicketComments_TicketCommentId", x => x.TicketCommentId,
                    "TicketComments", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.Sql(
            "INSERT INTO \"__TicketAttachments\" (\"Id\", \"TicketId\", \"TicketCommentId\", \"FileName\", \"StoredFileName\", \"ContentType\", \"FileSize\", \"UploadedByUserId\", \"UploadedByName\", \"UploadedByRole\", \"IsCustomerVisible\", \"CreatedAt\", \"UpdatedAt\") " +
            "SELECT \"Id\", \"TicketId\", NULL, \"FileName\", \"StoredFileName\", \"ContentType\", \"FileSize\", \"UploadedByUserId\", \"UploadedByName\", \"UploadedByRole\", \"IsCustomerVisible\", \"CreatedAt\", \"UpdatedAt\" FROM \"TicketAttachments\";");

        migrationBuilder.DropTable(name: "TicketAttachments");
        migrationBuilder.RenameTable(name: "__TicketAttachments", newName: "TicketAttachments");
        migrationBuilder.CreateIndex("IX_TicketAttachments_TicketId", "TicketAttachments", "TicketId");
        migrationBuilder.CreateIndex("IX_TicketAttachments_TicketCommentId", "TicketAttachments", "TicketCommentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "__TicketAttachments",
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
                table.PrimaryKey("PK___TicketAttachments", x => x.Id);
                table.ForeignKey("FK_TicketAttachments_Tickets_TicketId", x => x.TicketId,
                    "Tickets", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            "INSERT INTO \"__TicketAttachments\" (\"Id\", \"TicketId\", \"FileName\", \"StoredFileName\", \"ContentType\", \"FileSize\", \"UploadedByUserId\", \"UploadedByName\", \"UploadedByRole\", \"IsCustomerVisible\", \"CreatedAt\", \"UpdatedAt\") " +
            "SELECT \"Id\", \"TicketId\", \"FileName\", \"StoredFileName\", \"ContentType\", \"FileSize\", \"UploadedByUserId\", \"UploadedByName\", \"UploadedByRole\", \"IsCustomerVisible\", \"CreatedAt\", \"UpdatedAt\" FROM \"TicketAttachments\";");

        migrationBuilder.DropTable(name: "TicketAttachments");
        migrationBuilder.RenameTable(name: "__TicketAttachments", newName: "TicketAttachments");
        migrationBuilder.CreateIndex("IX_TicketAttachments_TicketId", "TicketAttachments", "TicketId");
    }
}
