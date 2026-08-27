using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMonthlyPlatformCommissionSummaryFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO MonthlyPlatformCommissionSummaries (Id, ClassId, TutorId, InstituteId, [Month], [Year], PlatformInstituteCommission, PlatformTutorCommission, PlatformAllCommission, LastUpdated)
                SELECT 
                    NEWID(),
                    cp.ClassId,
                    c.TutorId,
                    cp.InstituteId,
                    MONTH(cp.PaidAt),
                    YEAR(cp.PaidAt),
                    SUM(ISNULL(cp.InstituteCommission, 0)),
                    SUM(ISNULL(cp.TutorCommission, 0)),
                    SUM(ISNULL(cp.TotalPlatformAmount, 0)),
                    GETUTCDATE()
                FROM ClassPayments cp
                JOIN Classes c ON cp.ClassId = c.ClassId
                GROUP BY cp.ClassId, c.TutorId, cp.InstituteId, MONTH(cp.PaidAt), YEAR(cp.PaidAt)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MonthlyPlatformCommissionSummaries;");
        }
    }
}
