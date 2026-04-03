using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Financials
{
    /// <summary>
    /// Sent from frontend when saving bank details.
    /// These values are encrypted server-side before storage.
    /// NEVER log or return these raw values.
    /// </summary>
    public class SaveBankDetailsDto
    {
        [Required]
        public int BankCode { get; set; }

        [Required]
        [MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public int BranchCode { get; set; }

        [Required]
        [MaxLength(150)]
        public string BranchName { get; set; } = string.Empty;

        /// <summary>Account number — will be AES-256 encrypted before saving to DB.</summary>
        [Required]
        [MinLength(6)]
        [MaxLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>Full name of account holder.</summary>
        [Required]
        [MaxLength(200)]
        public string AccountHolderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sent from frontend after mock PayHere "tokenization".
    /// We NEVER receive or store the real card number — only the token + last4 + brand.
    /// The CVV is never transmitted to our backend.
    /// </summary>
    public class SaveCardTokenDto
    {
        /// <summary>Mock PayHere token, e.g. "payhere_mock_token_abc123".</summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>Last 4 digits of the card — safe for display.</summary>
        [Required]
        [MaxLength(4)]
        public string Last4 { get; set; } = string.Empty;

        /// <summary>Card network brand, e.g. "Visa" or "Mastercard".</summary>
        [Required]
        [MaxLength(30)]
        public string Brand { get; set; } = string.Empty;

        /// <summary>Cardholder name for display on the card widget.</summary>
        [MaxLength(200)]
        public string CardholderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// The ONLY financial data returned to the UI.
    /// Contains masked values — never the raw account number or full token.
    /// </summary>
    public class FinancialSummaryDto
    {
        // Bank details
        public bool HasBankDetails { get; set; }
        public string? MaskedAccountNumber { get; set; }  // "**** **** 5678"
        public string? BankName { get; set; }              // Safe to display
        public string? BranchName { get; set; }            // Safe to display
        public string? AccountHolderName { get; set; }     // Safe to display
        public int? BankCode { get; set; }                 // For re-populating dropdown
        public int? BranchCode { get; set; }               // For re-populating dropdown

        // Card details
        public bool HasCard { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? CardholderName { get; set; }
    }

    /// <summary>Bank entry for UI dropdown.</summary>
    public class BankDto
    {
        public int BankCode { get; set; }
        public string BankName { get; set; } = string.Empty;
    }

    /// <summary>Branch entry for UI dropdown (filtered by BankCode).</summary>
    public class BranchDto
    {
        public int BranchId { get; set; }
        public int BankCode { get; set; }
        public int BranchCode { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }
}
