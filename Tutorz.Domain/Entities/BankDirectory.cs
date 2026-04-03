using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    /// <summary>
    /// Sri Lankan bank loaded from LankaPay directory.
    /// </summary>
    public class Bank
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Use LankaPay BankCode as PK
        public int BankCode { get; set; }

        [Required]
        [MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }

    /// <summary>
    /// Branch of a Sri Lankan bank loaded from LankaPay directory.
    /// </summary>
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        public int BankCode { get; set; }

        [ForeignKey(nameof(BankCode))]
        public virtual Bank Bank { get; set; } = null!;

        public int BranchCode { get; set; }

        [MaxLength(150)]
        public string BranchName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string District { get; set; } = string.Empty;
    }
}
