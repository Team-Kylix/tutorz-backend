using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyPlatformCommissionSummaryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyPlatformCommissionSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstituteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PlatformInstituteCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformTutorCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformAllCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPlatformCommissionSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPlatformCommissionSummaries_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyPlatformCommissionSummaries_Institutes_InstituteId",
                        column: x => x.InstituteId,
                        principalTable: "Institutes",
                        principalColumn: "InstituteId");
                    table.ForeignKey(
                        name: "FK_MonthlyPlatformCommissionSummaries_Tutors_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutors",
                        principalColumn: "TutorId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlatformCommissionSummaries_ClassId_Month_Year",
                table: "MonthlyPlatformCommissionSummaries",
                columns: new[] { "ClassId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlatformCommissionSummaries_InstituteId",
                table: "MonthlyPlatformCommissionSummaries",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlatformCommissionSummaries_TutorId",
                table: "MonthlyPlatformCommissionSummaries",
                column: "TutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyPlatformCommissionSummaries");
        }
    }
}
