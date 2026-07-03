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
    /// Sent from frontend after PayHere tokenization — we NEVER receive or store the real
    /// card number. The PayHere customer_token + masked card metadata is all we store.
    /// The CVV is never transmitted to our backend.
    /// </summary>
    public class SaveCardTokenDto
    {
        /// <summary>PayHere customer_token returned from preapproval notify_url.</summary>
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

        /// <summary>Card expiry in MMYY format e.g. "0128", for display only.</summary>
        [MaxLength(10)]
        public string? CardExpiry { get; set; }
    }

    /// <summary>
    /// Notification payload sent from PayHere server to our preapproval notify_url.
    /// Contains the encrypted customer_token used for automated charging.
    /// </summary>
    public class PreapprovalNotifyDto
    {
        public string? merchant_id { get; set; }
        public string? order_id { get; set; }
        public string? payment_id { get; set; }
        public string? payhere_amount { get; set; }
        public string? payhere_currency { get; set; }
        public string? status_code { get; set; }
        public string? status_message { get; set; }
        public string? md5sig { get; set; }
        public string? method { get; set; }         // e.g. "VISA", "MASTER"
        public string? card_holder_name { get; set; }
        public string? card_no { get; set; }        // masked e.g. "************4242"
        public string? card_expiry { get; set; }    // MMYY e.g. "0128"
        public string? customer_token { get; set; } // The encrypted reusable token
        public string? custom_1 { get; set; }       // We use this for studentId
        public string? custom_2 { get; set; }
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
        public string? CardExpiry { get; set; }            // MMYY for display e.g. "0128"
    }

    /// <summary>Bank entry for UI dropdown.</summary>
    public class BankDto
    {
        public int BankCode { get; set; }
        public string BankName { get; set; } = string.Empty;
    }

    public class BranchDto
    {
        public int BranchId { get; set; }
        public int BankCode { get; set; }
        public int BranchCode { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }

    public class InitiatePaymentRequestDto
    {
        [Required]
        public Guid ClassId { get; set; }
        
        [Required]
        public int Month { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public decimal Amount { get; set; }

        public bool UseSavedCard { get; set; } = false;
    }

    public class InitiateBillPaymentRequestDto
    {
        [Required]
        public Guid BillId { get; set; }
        
        public bool UseSavedCard { get; set; } = false;
    }

    public class PayHereNotifyDto
    {
        public string? merchant_id { get; set; }
        public string? order_id { get; set; }
        public string? payment_id { get; set; }
        public string? payhere_amount { get; set; }
        public string? payhere_currency { get; set; }
        public string? status_code { get; set; }
        public string? md5sig { get; set; }
        public string? method { get; set; }
        public string? status_message { get; set; }
    }
}
