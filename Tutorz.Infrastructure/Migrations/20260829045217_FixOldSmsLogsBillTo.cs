using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    public partial class FixOldSmsLogsBillTo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.BillTo = t.UserId
                FROM SmsLogs s
                INNER JOIN Users u ON u.PhoneNumber = s.ReceiverPhoneNumber
                INNER JOIN Students st ON st.UserId = u.UserId
                INNER JOIN ClassPayments cp ON cp.StudentId = st.StudentId
                    AND ABS(DATEDIFF(minute, s.SentAt, cp.PaidAt)) < 5
                INNER JOIN Classes c ON c.ClassId = cp.ClassId
                INNER JOIN Tutors t ON t.TutorId = c.TutorId
                WHERE s.MessageContent LIKE '%your payment of LKR%'
                  AND s.BillTo != t.UserId;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
