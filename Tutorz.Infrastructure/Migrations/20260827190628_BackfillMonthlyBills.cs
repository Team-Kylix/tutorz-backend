using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMonthlyBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH IndivCommissions AS (
                    SELECT 
                        t.UserId, 
                        m.[Month], 
                        m.[Year], 
                        1 AS IsIndividual, 
                        SUM(m.PlatformAllCommission) AS CommissionCost
                    FROM MonthlyPlatformCommissionSummaries m
                    JOIN Tutors t ON m.TutorId = t.TutorId
                    WHERE m.InstituteId IS NULL
                    GROUP BY t.UserId, m.[Month], m.[Year]
                ),
                InstCommissions AS (
                    SELECT 
                        i.UserId, 
                        m.[Month], 
                        m.[Year], 
                        0 AS IsIndividual, 
                        SUM(m.PlatformAllCommission) AS CommissionCost
                    FROM MonthlyPlatformCommissionSummaries m
                    JOIN Institutes i ON m.InstituteId = i.InstituteId
                    WHERE m.InstituteId IS NOT NULL
                    GROUP BY i.UserId, m.[Month], m.[Year]
                ),
                AllCommissions AS (
                    SELECT * FROM IndivCommissions
                    UNION ALL
                    SELECT * FROM InstCommissions
                ),
                AllUsages AS (
                    SELECT
                        UserId,
                        [Month],
                        [Year],
                        CASE WHEN UserRole = 'Tutor' THEN 1 ELSE 0 END AS IsIndividual,
                        TotalCost AS UsageCost
                    FROM MonthlyUsageSummaries
                ),
                Combined AS (
                    SELECT 
                        COALESCE(c.UserId, u.UserId) AS UserId,
                        COALESCE(c.[Month], u.[Month]) AS [Month],
                        COALESCE(c.[Year], u.[Year]) AS [Year],
                        COALESCE(c.IsIndividual, u.IsIndividual) AS IsIndividual,
                        ISNULL(c.CommissionCost, 0) + ISNULL(u.UsageCost, 0) AS BillAmount
                    FROM AllCommissions c
                    FULL OUTER JOIN AllUsages u 
                        ON c.UserId = u.UserId 
                        AND c.[Month] = u.[Month] 
                        AND c.[Year] = u.[Year]
                        AND c.IsIndividual = u.IsIndividual
                )
                INSERT INTO MonthlyBills (Id, BillNumber, UserId, [Month], [Year], BillAmount, DueAmount, PaidAmount, IsPaid, IsIndividual, CreatedAt)
                SELECT 
                    NEWID(),
                    UPPER(RIGHT(NEWID(), 6)), 
                    UserId,
                    [Month],
                    [Year],
                    BillAmount,
                    0, 
                    0, 
                    0, 
                    IsIndividual,
                    GETUTCDATE()
                FROM Combined
                WHERE BillAmount > 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MonthlyBills;");
        }
    }
}
