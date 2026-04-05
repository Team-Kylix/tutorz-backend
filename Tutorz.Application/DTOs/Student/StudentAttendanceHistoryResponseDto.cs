using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Student
{
    public class StudentHistoryClassRowDto
    {
        public Guid Id { get; set; } // ClassId
        public string Name { get; set; } = string.Empty; // Class Name
        public string RegNo { get; set; } = string.Empty; // Tutor Name
        public string Mobile { get; set; } = string.Empty; // Additional info / empty

        // Maps Date to IsPresent
        public Dictionary<DateTime, bool> AttendanceRecord { get; set; } = new Dictionary<DateTime, bool>();
    }

    public class StudentAttendanceHistoryResponseDto
    {
        // Dates dictate the columns in the frontend
        public List<DateTime> ConductedDates { get; set; } = new List<DateTime>();
        
        // Rows of classes the student is in
        public List<StudentHistoryClassRowDto> Classes { get; set; } = new List<StudentHistoryClassRowDto>();

        // Pagination metadata
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // --- Summary Statistics ---
        public int DaysHeld { get; set; }
        public int DaysAttended { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}
