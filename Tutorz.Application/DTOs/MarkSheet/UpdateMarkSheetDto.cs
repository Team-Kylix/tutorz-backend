using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class UpdateMarkSheetDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public List<StudentMarkDto> Marks { get; set; } = new List<StudentMarkDto>();
    }
}
