using System.Collections.Generic;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.DTOs.Payment
{
    public class FinancialHistoryResponseDto
    {
        public PaginatedResultDto<ClassPaymentHistoryDto> PaginatedPayments { get; set; }
        
        public decimal TotalReceived { get; set; }
        public decimal TeacherShare { get; set; }
        public decimal InstituteShare { get; set; }
        public int TotalStudents { get; set; }
    }
}
