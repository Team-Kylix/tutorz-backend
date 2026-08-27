using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMonthlyUsageSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH SmsCosts AS (
                    SELECT 
                        BillTo AS UserId,
                        MONTH(SentAt) AS [Month],
                        YEAR(SentAt) AS [Year],
                        SUM(Cost) AS TotalSmsCost
                    FROM SmsLogs
                    WHERE BillTo IS NOT NULL
                    GROUP BY BillTo, MONTH(SentAt), YEAR(SentAt)
                ),
                AttendanceCosts AS (
                    SELECT
                        t.UserId AS UserId,
                        MONTH(a.Date) AS [Month],
                        YEAR(a.Date) AS [Year],
                        COUNT(*) * 1.5 AS TotalServerCost
                    FROM Attendances a
                    JOIN Tutors t ON a.TutorId = t.TutorId
                    WHERE a.TutorId IS NOT NULL
                    GROUP BY t.UserId, MONTH(a.Date), YEAR(a.Date)
                ),
                CombinedCosts AS (
                    SELECT 
                        COALESCE(s.UserId, a.UserId) AS UserId,
                        COALESCE(s.[Month], a.[Month]) AS [Month],
                        COALESCE(s.[Year], a.[Year]) AS [Year],
                        COALESCE(s.TotalSmsCost, 0) AS SmsCost,
                        COALESCE(a.TotalServerCost, 0) AS ServerCost
                    FROM SmsCosts s
                    FULL OUTER JOIN AttendanceCosts a 
                        ON s.UserId = a.UserId 
                        AND s.[Month] = a.[Month] 
                        AND s.[Year] = a.[Year]
                )
                INSERT INTO MonthlyUsageSummaries (Id, UserId, UserRole, [Month], [Year], SmsCost, ServerCost, TotalCost, LastUpdated)
                SELECT 
                    NEWID(),
                    c.UserId,
                    CASE 
                        WHEN t.TutorId IS NOT NULL THEN 'Tutor' 
                        WHEN i.InstituteId IS NOT NULL THEN 'Institute' 
                        ELSE 'Unknown' 
                    END AS UserRole,
                    c.[Month],
                    c.[Year],
                    c.SmsCost,
                    c.ServerCost,
                    (c.SmsCost + c.ServerCost) AS TotalCost,
                    GETUTCDATE()
                FROM CombinedCosts c
                LEFT JOIN Tutors t ON c.UserId = t.UserId
                LEFT JOIN Institutes i ON c.UserId = i.UserId
                WHERE NOT EXISTS (
                    SELECT 1 FROM MonthlyUsageSummaries m 
                    WHERE m.UserId = c.UserId AND m.[Month] = c.[Month] AND m.[Year] = c.[Year]
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MonthlyUsageSummaries;");
        }
    }
}
