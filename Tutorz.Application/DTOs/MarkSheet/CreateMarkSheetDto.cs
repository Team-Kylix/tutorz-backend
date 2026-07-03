using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class CreateMarkSheetDto
    {
        public Guid? InstituteId { get; set; }

        [Required]
        public Guid ClassId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public List<StudentMarkDto> Marks { get; set; } = new List<StudentMarkDto>();
    }
}
