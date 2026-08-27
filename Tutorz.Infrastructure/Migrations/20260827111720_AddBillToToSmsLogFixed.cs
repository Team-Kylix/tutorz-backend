using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillToToSmsLogFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillTo",
                table: "SmsLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsLogs_BillTo",
                table: "SmsLogs",
                column: "BillTo");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsLogs_Users_BillTo",
                table: "SmsLogs",
                column: "BillTo",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmsLogs_Users_BillTo",
                table: "SmsLogs");

            migrationBuilder.DropIndex(
                name: "IX_SmsLogs_BillTo",
                table: "SmsLogs");

            migrationBuilder.DropColumn(
                name: "BillTo",
                table: "SmsLogs");
        }
    }
}
