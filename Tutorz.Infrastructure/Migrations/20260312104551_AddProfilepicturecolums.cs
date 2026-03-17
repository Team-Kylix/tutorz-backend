using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilepicturecolums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlLarge",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlSmall",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlLarge",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlSmall",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlLarge",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrlSmall",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrlLarge",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrlSmall",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrlLarge",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrlSmall",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrlLarge",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrlSmall",
                table: "Institutes");
        }
    }
}
