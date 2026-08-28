using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Billing
{
    public class InstituteBillPdfDto
    {
        public string BillNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Institute";
        public string InstituteName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? RegistrationNumber { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        
        public DateTime GeneratedAt { get; set; }
        
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        public decimal SmsTotalCost { get; set; }
        public decimal ServerTotalCost { get; set; }
        
        public decimal PreviousOverdueAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalPayable { get; set; }
        
        public List<TutorCommissionGroupDto> TutorCommissions { get; set; } = new();
    }

    public class TutorCommissionGroupDto
    {
        public string TutorName { get; set; } = string.Empty;
        public List<ClassCommissionDto> Classes { get; set; } = new();
    }

    public class ClassCommissionDto
    {
        public string ClassName { get; set; } = string.Empty;
        public decimal Charge { get; set; }
    }
}
    
