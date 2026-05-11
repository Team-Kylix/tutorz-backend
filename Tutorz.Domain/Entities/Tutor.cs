using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Tutor
    {
        public Guid TutorId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public String FirstName { get; set; } = string.Empty;
        public String LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public String Bio { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }

        // --- Financial Details (Field-Level Encrypted via AES-256) ---
        // Legacy plain fields kept for backward compat during migration
        public string? BankAccountNumber { get; set; }  // LEGACY - will be null after migration
        public string? BankName { get; set; }            // LEGACY - will be null after migration

        // Encrypted replacements
        public string? EncryptedBankName { get; set; }
        public string? EncryptedBranchName { get; set; }
        public string? EncryptedAccountNumber { get; set; }
        public string? EncryptedAccountHolderName { get; set; }
        public int? BankCode { get; set; }            // Plaintext — for dropdown re-selection
        public int? BranchCode { get; set; }          // Plaintext — for branch re-selection
        public string? MaskedAccountNumber { get; set; } // e.g. "**** **** 5678" — safe for UI

        // --- Card / Mock PayHere Token ---
        public string? PayHereToken { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? CardholderName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }

        public virtual ICollection<InstituteTutor> InstituteTutors { get; set; } = new List<InstituteTutor>();
    }
}
