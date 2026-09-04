using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId_InstituteId_IsIndividual",
                table: "Wallets",
                columns: new[] { "UserId", "InstituteId", "IsIndividual" },
                unique: true,
                filter: "[InstituteId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId_InstituteId_IsIndividual",
                table: "Wallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);
        }
    }
}
