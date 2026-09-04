using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEarningsSummaryAndWalletChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstituteId",
                table: "WalletTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstituteId",
                table: "Wallets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndividual",
                table: "Wallets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EarningsSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    InstituteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstituteCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SmsDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServerDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningsSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EarningsSummaries_Institutes_InstituteId",
                        column: x => x.InstituteId,
                        principalTable: "Institutes",
                        principalColumn: "InstituteId");
                    table.ForeignKey(
                        name: "FK_EarningsSummaries_Tutors_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutors",
                        principalColumn: "TutorId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_InstituteId",
                table: "WalletTransactions",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_InstituteId",
                table: "Wallets",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_EarningsSummaries_InstituteId",
                table: "EarningsSummaries",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_EarningsSummaries_TutorId",
                table: "EarningsSummaries",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_Institutes_InstituteId",
                table: "Wallets",
                column: "InstituteId",
                principalTable: "Institutes",
                principalColumn: "InstituteId");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_Institutes_InstituteId",
                table: "WalletTransactions",
                column: "InstituteId",
                principalTable: "Institutes",
                principalColumn: "InstituteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_Institutes_InstituteId",
                table: "Wallets");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_Institutes_InstituteId",
                table: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "EarningsSummaries");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_InstituteId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_InstituteId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "InstituteId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "InstituteId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "IsIndividual",
                table: "Wallets");
        }
    }
}
