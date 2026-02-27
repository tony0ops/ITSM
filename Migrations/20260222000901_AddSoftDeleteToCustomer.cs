using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Tickets",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Tickets",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Tickets",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateTable(
                name: "CustomerUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CanCreateTickets = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewTickets = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAddComments = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerUsers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsInternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCustomerVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedGroup",
                table: "Tickets",
                column: "AssignedGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedAt",
                table: "Tickets",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IsDeleted",
                table: "Tickets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Severity",
                table: "Tickets",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivities_CreatedAt",
                table: "TicketActivities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSlas_CustomerId_Unique",
                table: "CustomerSlas",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSlas_IsActive",
                table: "CustomerSlas",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUsers_CustomerId",
                table: "CustomerUsers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUsers_IsActive",
                table: "CustomerUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUsers_UserId_Unique",
                table: "CustomerUsers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Group",
                table: "Teams",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_CreatedAt",
                table: "TicketComments",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_IsInternal",
                table: "TicketComments",
                column: "IsInternal");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId",
                table: "TicketComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TeamId",
                table: "UserProfiles",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId_Unique",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSlas");

            migrationBuilder.DropTable(
                name: "CustomerUsers");

            migrationBuilder.DropTable(
                name: "TicketComments");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedGroup",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_IsDeleted",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Severity",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketActivities_CreatedAt",
                table: "TicketActivities");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Customers");
        }
    }
}
