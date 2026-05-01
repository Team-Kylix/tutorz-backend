namespace Tutorz.Application.DTOs.Billing
{
    /// <summary>
    /// Lightweight row used in paginated list views (Admin all-bills, User my-bills).
    /// </summary>
    public class BillSummaryDto
    {
        public Guid BillId { get; set; }
        public string BillReference { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYear { get; set; } = string.Empty;
        public decimal PayableAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Full detail used in bill detail modal and PDF generation.
    /// </summary>
    public class BillDetailDto
    {
        public Guid BillId { get; set; }
        public string BillReference { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? Address { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYear { get; set; } = string.Empty;
        public DateTime BillStartDate { get; set; }
        public DateTime BillEndDate { get; set; }
        public DateTime GeneratedAt { get; set; }

        // Line Items
        public int ApiCallCount { get; set; }
        public decimal ApiCallRate { get; set; }
        public decimal ApiUsageAmount { get; set; }

        public int SmsSentCount { get; set; }
        public decimal SmsRate { get; set; }
        public decimal SmsAmount { get; set; }

        public decimal PlatformCommissionAmount { get; set; }
        public decimal PreviousOverdueAmount { get; set; }

        // Totals
        public decimal SubTotal { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal PayableAmount { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }

        // Dynamic Line Items for Platform Commission
        public decimal PlatformCommissionRate { get; set; }
        public List<ClassCommissionItemDto> ClassCommissions { get; set; } = new();
    }

    /// <summary>
    /// Represents a single class's contribution to the platform commission.
    /// </summary>
    public class ClassCommissionItemDto
    {
        public string ClassName { get; set; } = string.Empty; // e.g. "Grade 7 Science Samadi"
        public decimal Earnings { get; set; } // The Qty (TuitionAmount or InstituteAmount)
        public decimal Rate { get; set; } // The commission % (e.g. 1%)
        public decimal Amount { get; set; } // The actual fee (Earnings * Rate)
    }

    /// <summary>Request body for manually triggering bill generation.</summary>
    public class GenerateBillsRequestDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

    /// <summary>Admin-editable billing configuration stored in AppSettings.</summary>
    public class BillingConfigDto
    {
        /// <summary>Cost per API call in LKR (default 0.0100)</summary>
        public decimal ApiCallRate { get; set; } = 0.01m;

        /// <summary>Cost per SMS in LKR (default 2.0000)</summary>
        public decimal SmsRate { get; set; } = 2.00m;

        /// <summary>Platform commission % charged from each party (default 1.00)</summary>
        public decimal PlatformCommissionRate { get; set; } = 1.00m;

        /// <summary>VAT percentage applied to sub-total (default 0, set to 18 if applicable)</summary>
        public decimal VatPercentage { get; set; } = 0m;

        /// <summary>SSCL percentage (default 0)</summary>
        public decimal SsclPercentage { get; set; } = 0m;
    }

    /// <summary>Response wrapper for paginated bill lists.</summary>
    public class BillPagedResult
    {
        public List<BillSummaryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => (Page * PageSize) < TotalCount;
    }
}
