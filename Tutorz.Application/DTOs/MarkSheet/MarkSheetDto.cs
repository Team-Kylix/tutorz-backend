using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.MarkSheet
{
    public class MarkSheetDto
    {
        public Guid MarkSheetId { get; set; }
        public string ReferenceNumber { get; set; }
        public Guid? InstituteId { get; set; }
        public string InstituteName { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public string Grade { get; set; }
        public string Subject { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<MarkRecordDto> MarkRecords { get; set; } = new List<MarkRecordDto>();
    }
}
