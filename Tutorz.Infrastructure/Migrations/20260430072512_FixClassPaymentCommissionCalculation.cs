using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixClassPaymentCommissionCalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ClassPayments
                SET InstituteCommission = ROUND(AmountPaid * 0.0025, 2),
                    TutorCommission = ROUND(AmountPaid * 0.0075, 2),
                    TotalPlatformAmount = ROUND(AmountPaid * 0.01, 2)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
