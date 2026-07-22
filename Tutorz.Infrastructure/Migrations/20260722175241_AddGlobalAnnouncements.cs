using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemAnnouncements",
                columns: table => new
                {
                    AnnouncementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAnnouncements", x => x.AnnouncementId);
                });

            migrationBuilder.CreateTable(
                name: "UserAnnouncementReads",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAnnouncementReads", x => new { x.UserId, x.AnnouncementId });
                    table.ForeignKey(
                        name: "FK_UserAnnouncementReads_SystemAnnouncements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "SystemAnnouncements",
                        principalColumn: "AnnouncementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAnnouncementReads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAnnouncementReads_AnnouncementId",
                table: "UserAnnouncementReads",
                column: "AnnouncementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAnnouncementReads");

            migrationBuilder.DropTable(
                name: "SystemAnnouncements");
        }
    }
}
