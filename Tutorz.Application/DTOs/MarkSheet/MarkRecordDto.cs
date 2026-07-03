using System;
using Tutorz.Domain.Enums;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class MarkRecordDto
    {
        public Guid MarkRecordId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string RegistrationNumber { get; set; }
        public decimal Marks { get; set; }
        public string Medal { get; set; } // None, Gold, Silver, Bronze
    }
}
