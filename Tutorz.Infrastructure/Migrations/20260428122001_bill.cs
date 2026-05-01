using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class bill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InstituteAmount",
                table: "ClassPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstituteCommission",
                table: "ClassPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstituteCommissionPercentage",
                table: "ClassPayments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPlatformAmount",
                table: "ClassPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TuitionAmount",
                table: "ClassPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TutorCommission",
                table: "ClassPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstituteCommissionRate",
                table: "Classes",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BillReference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    MonthYear = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    BillStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApiCallCount = table.Column<int>(type: "int", nullable: false),
                    ApiCallRate = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ApiUsageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SmsSentCount = table.Column<int>(type: "int", nullable: false),
                    SmsRate = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    SmsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformCommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousOverdueAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.BillId);
                    table.ForeignKey(
                        name: "FK_Bills_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_UserId_Month_Year",
                table: "Bills",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropColumn(
                name: "InstituteAmount",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "InstituteCommission",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "InstituteCommissionPercentage",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "TotalPlatformAmount",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "TuitionAmount",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "TutorCommission",
                table: "ClassPayments");

            migrationBuilder.DropColumn(
                name: "InstituteCommissionRate",
                table: "Classes");
        }
    }
}
