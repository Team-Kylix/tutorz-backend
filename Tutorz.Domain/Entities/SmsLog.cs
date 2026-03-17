using System;

namespace Tutorz.Domain.Entities
{
    public class SmsLog
    {
        public Guid SmsLogId { get; set; }
        public Guid? SenderUserId { get; set; }
        public string ReceiverPhoneNumber { get; set; }
        public string MessageContent { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } // "Sent", "Failed"
        public decimal Cost { get; set; }
        public string ErrorMessage { get; set; }

        // Navigation Property (Optional)
        public User SenderUser { get; set; }
    }
}
