using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Institute
    {
        public Guid InstituteId { get; set; }
        public string RegistrationNumber { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string? Website { get; set; }
        public bool IsSmsEnabled { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }

        // --- Financial Details (Field-Level Encrypted via AES-256) ---
        // These columns store Base64-encoded AES ciphertext. Devs cannot read them without the server key.
        public string? EncryptedBankName { get; set; }
        public string? EncryptedBranchName { get; set; }
        public string? EncryptedAccountNumber { get; set; }
        public string? EncryptedAccountHolderName { get; set; }
        public int? BankCode { get; set; }            // Plaintext — used to re-select bank in dropdown
        public int? BranchCode { get; set; }          // Plaintext — used to re-select branch
        public string? MaskedAccountNumber { get; set; } // e.g. "**** **** 5678" — safe for UI

        // --- Card / Mock PayHere Token ---
        public string? PayHereToken { get; set; }     // e.g. "payhere_mock_token_abc123"
        public string? CardLast4 { get; set; }         // e.g. "5678"
        public string? CardBrand { get; set; }         // e.g. "Visa" / "Mastercard"
        public string? CardholderName { get; set; }    // Display name on card

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionPercentage { get; set; } = 0;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }

        public virtual ICollection<InstituteStudent> InstituteStudents { get; set; } = new List<InstituteStudent>();
        public virtual ICollection<InstituteTutor> InstituteTutors { get; set; } = new List<InstituteTutor>();
    }
}