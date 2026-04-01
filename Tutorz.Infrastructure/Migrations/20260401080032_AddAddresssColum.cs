using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddresssColum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Students");
        }
    }
}
