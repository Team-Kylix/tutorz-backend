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

        /// <summary>
        /// Generates a PDF invoice for a class payment, validated by the tutor who owns the class.
        /// Returns null if the payment does not belong to a class owned by this tutor.
        /// </summary>
        Task<byte[]?> GenerateClassPaymentPdfForTutorAsync(Guid paymentId, Guid tutorId);

        /// <summary>
        /// Generates a PDF invoice for a class payment, validated by the institute that owns the class.
        /// Returns null if the payment does not belong to the requesting institute.
        /// </summary>
        Task<byte[]?> GenerateClassPaymentPdfForInstituteAsync(Guid paymentId, Guid instituteId);
    
        /// <summary>
        /// Generates a PDF invoice for a class payment without owner validation, for system admins.
        /// </summary>
        Task<byte[]?> GenerateClassPaymentPdfForSystemAsync(Guid paymentId);
    }
}
