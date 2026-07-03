using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Institute
{
    public class StudentAttendanceRowDto
    {
        public Guid StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;

        // Maps Date -> IsPresent (only for dates the student was marked present)
        public Dictionary<DateTime, bool> AttendanceRecord { get; set; } = new Dictionary<DateTime, bool>();

        // Dates when this student's specific class(es) were conducted.
        // Any date here where IsPresent=false should be displayed as "Absent" (red cross).
        public List<DateTime> ClassConductedDates { get; set; } = new List<DateTime>();
    }

    public class AttendanceHistoryResponseDto
    {
        // Dates dictate the columns in the frontend
        public List<DateTime> ConductedDates { get; set; } = new List<DateTime>();
        
        // Rows of student attendance
        public List<StudentAttendanceRowDto> Students { get; set; } = new List<StudentAttendanceRowDto>();

        // Pagination metadata
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // --- Summary Statistics ---
        public decimal TotalReceived { get; set; }
        public decimal TotalDue { get; set; }
        public int TotalStudentCount { get; set; }
    }
}
