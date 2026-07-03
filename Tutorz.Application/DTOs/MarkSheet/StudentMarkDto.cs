using System;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class StudentMarkDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public decimal Marks { get; set; }
    }
}
