using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeAdminAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedAdminUserId",
                table: "Disputes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_AssignedAdminUserId",
                table: "Disputes",
                column: "AssignedAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_Users_AssignedAdminUserId",
                table: "Disputes",
                column: "AssignedAdminUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_Users_AssignedAdminUserId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_AssignedAdminUserId",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "AssignedAdminUserId",
                table: "Disputes");
        }
    }
}
