namespace Tutorz.Application.DTOs.Tutor
{
    public class TutorDashboardStatsDto
    {
        public int TotalStudents { get; set; }
        public int ActiveClasses { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal PendingWithdrawals { get; set; }
    }
}
