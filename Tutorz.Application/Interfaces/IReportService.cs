using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Report;

namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// Provides tutor-scoped monthly report data and PDF generation.
    /// Implemented in a dedicated ReportService to keep concerns separate.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Returns the monthly report grid rows for a tutor.
        /// Each row = one (Month, Year) showing student/payment aggregate stats.
        /// Only months where at least one student attended ≥ 1 day are included.
        /// </summary>
        Task<ServiceResponse<TutorReportResponseDto>> GetTutorMonthlyReportAsync(
            TutorReportFilterDto filter);

        /// <summary>
        /// Generates a consolidated PDF for a specific month.
        /// PDF is always scoped to filter.Month + filter.Year.
        /// Groups content by Institute → Class with a student detail table per class.
        /// Uses a single bulk DB query pair (attendance + payments) for cost efficiency.
        /// Returns null if no data is found for the given scope.
        /// </summary>
        Task<byte[]?> GenerateTutorMonthlyReportPdfAsync(
            TutorReportFilterDto filter);
    }
}
