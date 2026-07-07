using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorIdToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TutorId",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TutorId",
                table: "Attendances",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Tutors_TutorId",
                table: "Attendances",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Tutors_TutorId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_TutorId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "TutorId",
                table: "Attendances");
        }
    }
}
