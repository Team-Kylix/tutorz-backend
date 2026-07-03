using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tutorz.Domain.Enums;

namespace Tutorz.Domain.Entities
{
    public class MarkRecord
    {
        [Key]
        public Guid MarkRecordId { get; set; }

        public Guid MarkSheetId { get; set; }
        [ForeignKey("MarkSheetId")]
        public virtual MarkSheet MarkSheet { get; set; }

        public Guid StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Marks { get; set; }

        public MedalType Medal { get; set; } = MedalType.None;
    }
}
