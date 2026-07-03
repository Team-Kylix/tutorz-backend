using System;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class StudentMarkRecordResponseDto
    {
        public Guid MarkRecordId { get; set; }
        public Guid MarkSheetId { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
        public string Subject { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal Marks { get; set; }
        public string Medal { get; set; }
        public DateTime Date { get; set; }
    }
}
