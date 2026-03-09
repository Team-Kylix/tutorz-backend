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

        // Maps Date to IsPresent
        public Dictionary<DateTime, bool> AttendanceRecord { get; set; } = new Dictionary<DateTime, bool>();
    }

    public class AttendanceHistoryResponseDto
    {
        // Dates dictate the columns in the frontend
        public List<DateTime> ConductedDates { get; set; } = new List<DateTime>();
        
        // Rows of student attendance
        public List<StudentAttendanceRowDto> Students { get; set; } = new List<StudentAttendanceRowDto>();
    }
}
