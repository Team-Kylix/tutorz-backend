using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_RegistrationNumber",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Institutions",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Students");

            migrationBuilder.RenameTable(
                name: "Institutions",
                newName: "Institutes");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Students",
                newName: "IsPrimary");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpires",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Institutes",
                table: "Institutes",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                table: "Students",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Users_UserId",
                table: "Students",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Users_UserId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_UserId",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Institutes",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpExpires",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Institutes",
                newName: "Institutions");

            migrationBuilder.RenameColumn(
                name: "IsPrimary",
                table: "Students",
                newName: "IsActive");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Institutions",
                table: "Institutions",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegistrationNumber",
                table: "Users",
                column: "RegistrationNumber",
                unique: true);
        }
    }
}
