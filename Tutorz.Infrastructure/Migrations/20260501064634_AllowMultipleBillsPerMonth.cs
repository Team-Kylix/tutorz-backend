using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleBillsPerMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_UserId_Month_Year",
                table: "Bills");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_UserId_Month_Year",
                table: "Bills",
                columns: new[] { "UserId", "Month", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_UserId_Month_Year",
                table: "Bills");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_UserId_Month_Year",
                table: "Bills",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);
        }
    }
}
