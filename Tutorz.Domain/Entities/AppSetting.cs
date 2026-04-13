using System;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Domain.Entities
{
    /// <summary>
    /// Key-value store for global application settings.
    /// Used by Layer 3 of the PWA update strategy:
    /// - Key "MinTokenDate" holds the UTC timestamp of the last forced logout.
    /// - Any JWT issued BEFORE this date is rejected with 401 Unauthorized.
    /// 
    /// To force all users to re-login on a new release:
    ///   UPDATE AppSettings SET Value = GETUTCDATE() WHERE Key = 'MinTokenDate'
    /// </summary>
    public class AppSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
