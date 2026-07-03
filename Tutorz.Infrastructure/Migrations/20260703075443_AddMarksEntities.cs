using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarksEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarkSheets",
                columns: table => new
                {
                    MarkSheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstituteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkSheets", x => x.MarkSheetId);
                    table.ForeignKey(
                        name: "FK_MarkSheets_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                    table.ForeignKey(
                        name: "FK_MarkSheets_Institutes_InstituteId",
                        column: x => x.InstituteId,
                        principalTable: "Institutes",
                        principalColumn: "InstituteId");
                    table.ForeignKey(
                        name: "FK_MarkSheets_Tutors_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutors",
                        principalColumn: "TutorId");
                });

            migrationBuilder.CreateTable(
                name: "MarkRecords",
                columns: table => new
                {
                    MarkRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarkSheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Medal = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkRecords", x => x.MarkRecordId);
                    table.ForeignKey(
                        name: "FK_MarkRecords_MarkSheets_MarkSheetId",
                        column: x => x.MarkSheetId,
                        principalTable: "MarkSheets",
                        principalColumn: "MarkSheetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarkRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarkRecords_MarkSheetId",
                table: "MarkRecords",
                column: "MarkSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkRecords_StudentId",
                table: "MarkRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkSheets_ClassId",
                table: "MarkSheets",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkSheets_InstituteId",
                table: "MarkSheets",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkSheets_ReferenceNumber",
                table: "MarkSheets",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkSheets_TutorId",
                table: "MarkSheets",
                column: "TutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkRecords");

            migrationBuilder.DropTable(
                name: "MarkSheets");
        }
    }
}
