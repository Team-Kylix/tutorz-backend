using System;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Institute
{
    public class MarkAttendanceDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid ClassId { get; set; }
        
        public DateTime? Date { get; set; }
    }
}
