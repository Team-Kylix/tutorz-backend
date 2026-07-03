using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Report
{
    /// <summary>One row in the tutor monthly report grid — one row per (Month, Year)</summary>
    public class TutorMonthReportRowDto
    {
        /// <summary>Unique reference for this month e.g. "RPT2604AB12"</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>Display string e.g. "April 2026"</summary>
        public string MonthYear { get; set; } = string.Empty;

        public int Month { get; set; }
        public int Year { get; set; }

        /// <summary>Scope description shown in grid e.g. "All Institutes · All Classes"</summary>
        public string DetailsPeriod { get; set; } = string.Empty;

        /// <summary>Students who attended at least 1 day in this month</summary>
        public int TotalStudents { get; set; }

        public int PaidCount { get; set; }
        public int UnpaidCount { get; set; }
    }

    /// <summary>Per-student detail row used inside the PDF report</summary>
    public class TutorReportStudentDetailDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }

        /// <summary>Number of days present in the scoped month</summary>
        public int AttendanceCount { get; set; }

        /// <summary>"Paid" or "Not Yet"</summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>Amount paid; null if not yet paid</summary>
        public decimal? PaidAmount { get; set; }
    }

    /// <summary>Grouping of students under a class inside the PDF</summary>
    public class TutorReportClassSectionDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string? InstituteName { get; set; }
        public List<TutorReportStudentDetailDto> Students { get; set; } = new();
    }

    /// <summary>Full response payload for GET /api/report/monthly</summary>
    public class TutorReportResponseDto
    {
        public List<TutorMonthReportRowDto> Rows { get; set; } = new();
    }

    /// <summary>Filter DTO passed between controller and service</summary>
    public class TutorReportFilterDto
    {
        public Guid TutorId { get; set; }
        public Guid? InstituteId { get; set; }
        public bool NoInstitute { get; set; }
        public Guid? ClassId { get; set; }

        /// <summary>Specific month for PDF (1–12). Null = all months.</summary>
        public int? Month { get; set; }

        /// <summary>Specific year for PDF. Null = all years.</summary>
        public int? Year { get; set; }
    }
}
