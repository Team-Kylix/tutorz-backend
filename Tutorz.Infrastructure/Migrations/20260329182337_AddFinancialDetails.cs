using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankCode",
                table: "Tutors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchCode",
                table: "Tutors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardholderName",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAccountHolderName",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAccountNumber",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedBankName",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedBranchName",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaskedAccountNumber",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayHereToken",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardholderName",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayHereToken",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankCode",
                table: "Institutes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchCode",
                table: "Institutes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardholderName",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAccountHolderName",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAccountNumber",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedBankName",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedBranchName",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaskedAccountNumber",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayHereToken",
                table: "Institutes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    BankCode = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.BankCode);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankCode = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_Branches_Banks_BankCode",
                        column: x => x.BankCode,
                        principalTable: "Banks",
                        principalColumn: "BankCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BankCode_BranchCode",
                table: "Branches",
                columns: new[] { "BankCode", "BranchCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "CardholderName",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "EncryptedAccountHolderName",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "EncryptedAccountNumber",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "EncryptedBankName",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "EncryptedBranchName",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "MaskedAccountNumber",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "PayHereToken",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CardholderName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PayHereToken",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "CardholderName",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "EncryptedAccountHolderName",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "EncryptedAccountNumber",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "EncryptedBankName",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "EncryptedBranchName",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "MaskedAccountNumber",
                table: "Institutes");

            migrationBuilder.DropColumn(
                name: "PayHereToken",
                table: "Institutes");
        }
    }
}
