using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceServerCostsRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                -- Zero out ServerCost in MonthlyUsageSummaries and subtract it from TotalCost
                UPDATE MonthlyUsageSummaries
                SET TotalCost = TotalCost - ISNULL(ServerCost, 0);

                -- Also subtract the old ServerCost from MonthlyBills
                UPDATE b
                SET b.BillAmount = b.BillAmount - ISNULL(u.ServerCost, 0)
                FROM MonthlyBills b
                JOIN MonthlyUsageSummaries u 
                    ON b.UserId = u.UserId 
                    AND b.[Month] = u.[Month] 
                    AND b.[Year] = u.[Year]
                    AND b.IsIndividual = (CASE WHEN u.UserRole = 'Tutor' THEN 1 ELSE 0 END);

                -- Finally zero out ServerCost
                UPDATE MonthlyUsageSummaries
                SET ServerCost = 0;

                -- Calculate Institute Cost (0.5 per attendance for Institute classes)
                WITH InstituteCosts AS (
                    SELECT 
                        i.UserId, 
                        MONTH(a.Date) AS [Month], 
                        YEAR(a.Date) AS [Year], 
                        COUNT(*) * 0.5 AS Cost
                    FROM Attendances a
                    JOIN Classes c ON a.ClassId = c.ClassId
                    JOIN Institutes i ON c.InstituteId = i.InstituteId
                    WHERE c.InstituteId IS NOT NULL
                    GROUP BY i.UserId, MONTH(a.Date), YEAR(a.Date)
                ),
                -- Calculate Tutor Cost for Institute classes (1.5 per attendance)
                TutorInstCosts AS (
                    SELECT 
                        t.UserId, 
                        MONTH(a.Date) AS [Month], 
                        YEAR(a.Date) AS [Year], 
                        COUNT(*) * 1.5 AS Cost
                    FROM Attendances a
                    JOIN Classes c ON a.ClassId = c.ClassId
                    JOIN Tutors t ON c.TutorId = t.TutorId
                    WHERE c.InstituteId IS NOT NULL
                    GROUP BY t.UserId, MONTH(a.Date), YEAR(a.Date)
                ),
                -- Calculate Tutor Cost for Individual classes (2.0 per attendance)
                TutorIndivCosts AS (
                    SELECT 
                        t.UserId, 
                        MONTH(a.Date) AS [Month], 
                        YEAR(a.Date) AS [Year], 
                        COUNT(*) * 2.0 AS Cost
                    FROM Attendances a
                    JOIN Classes c ON a.ClassId = c.ClassId
                    JOIN Tutors t ON c.TutorId = t.TutorId
                    WHERE c.InstituteId IS NULL
                    GROUP BY t.UserId, MONTH(a.Date), YEAR(a.Date)
                ),
                -- Aggregate all Tutor Costs
                TutorTotalCosts AS (
                    SELECT UserId, [Month], [Year], SUM(Cost) AS Cost
                    FROM (
                        SELECT * FROM TutorInstCosts
                        UNION ALL
                        SELECT * FROM TutorIndivCosts
                    ) temp
                    GROUP BY UserId, [Month], [Year]
                ),
                -- Combine everything
                CombinedNewCosts AS (
                    SELECT UserId, [Month], [Year], Cost, 'Institute' AS UserRole FROM InstituteCosts
                    UNION ALL
                    SELECT UserId, [Month], [Year], Cost, 'Tutor' AS UserRole FROM TutorTotalCosts
                )

                -- Apply the new costs to MonthlyUsageSummaries
                MERGE INTO MonthlyUsageSummaries AS target
                USING CombinedNewCosts AS source
                ON target.UserId = source.UserId 
                   AND target.[Month] = source.[Month] 
                   AND target.[Year] = source.[Year]
                WHEN MATCHED THEN
                    UPDATE SET 
                        target.ServerCost = source.Cost,
                        target.TotalCost = target.TotalCost + source.Cost
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (Id, UserId, UserRole, [Month], [Year], SmsCost, ServerCost, TotalCost, LastUpdated)
                    VALUES (NEWID(), source.UserId, source.UserRole, source.[Month], source.[Year], 0, source.Cost, source.Cost, GETUTCDATE());

                -- Apply the new costs back to MonthlyBills
                UPDATE b
                SET b.BillAmount = b.BillAmount + ISNULL(u.ServerCost, 0)
                FROM MonthlyBills b
                JOIN MonthlyUsageSummaries u 
                    ON b.UserId = u.UserId 
                    AND b.[Month] = u.[Month] 
                    AND b.[Year] = u.[Year]
                    AND b.IsIndividual = (CASE WHEN u.UserRole = 'Tutor' THEN 1 ELSE 0 END);
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
