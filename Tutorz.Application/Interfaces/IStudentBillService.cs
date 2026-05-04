using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// Generates student-facing class fee PDF invoices from ClassPayment records.
    /// </summary>
    public interface IStudentBillService
    {
        /// <summary>
        /// Generates a PDF invoice for a single paid class payment.
        /// Returns null if the payment does not belong to the requesting student.
        /// </summary>
        Task<byte[]?> GenerateClassPaymentPdfAsync(Guid paymentId, Guid studentId);
    }
}
