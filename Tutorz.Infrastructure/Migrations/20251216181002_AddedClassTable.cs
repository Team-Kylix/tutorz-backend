using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedClassTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Class_Tutors_TutorId",
                table: "Class");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Class_ClassId",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Class",
                table: "Class");

            migrationBuilder.RenameTable(
                name: "Class",
                newName: "Classes");

            migrationBuilder.RenameIndex(
                name: "IX_Class_TutorId",
                table: "Classes",
                newName: "IX_Classes_TutorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Classes",
                table: "Classes",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Tutors_TutorId",
                table: "Classes",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Tutors_TutorId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Classes",
                table: "Classes");

            migrationBuilder.RenameTable(
                name: "Classes",
                newName: "Class");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_TutorId",
                table: "Class",
                newName: "IX_Class_TutorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Class",
                table: "Class",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Class_Tutors_TutorId",
                table: "Class",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Class_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "Class",
                principalColumn: "ClassId");
        }
    }
}
