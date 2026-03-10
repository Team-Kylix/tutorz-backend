using System;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Payment
{
    /// <summary>Status of a student's payment for a specific class-month-year</summary>
    public class MonthPaymentStatusDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>"Paid", "Unpaid", or "Future"</summary>
        public string Status { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    /// <summary>Full payment record returned after recording a payment</summary>
    public class ClassPaymentDto
    {
        public Guid PaymentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public Guid InstituteId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Note { get; set; }
        // Denormalised for display
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string Subject { get; set; }
    }

    /// <summary>Request body for recording a class payment</summary>
    public class RecordPaymentRequest
    {
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal AmountPaid { get; set; }
        public string? Note { get; set; }
    }
}
