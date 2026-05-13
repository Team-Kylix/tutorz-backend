using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Api.Attributes;
using Tutorz.Application.DTOs.Report;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [Authorize(Roles = "Tutor")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ITutorRepository _tutorRepo;

        public ReportController(IReportService reportService, ITutorRepository tutorRepo)
        {
            _reportService = reportService;
            _tutorRepo = tutorRepo;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// GET /api/report/monthly
        /// Returns monthly report grid rows for the logged-in tutor.
        /// Each row = one (Month, Year) showing student/payment aggregate stats.
        /// </summary>
        [HttpGet("monthly")]
        [ApiPurpose("Get Tutor Monthly Report Grid")]
        public async Task<IActionResult> GetMonthlyReport(
            [FromQuery] Guid? instituteId,
            [FromQuery] bool noInstitute = false,
            [FromQuery] Guid? classId = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return NotFound(new { message = "Tutor profile not found." });

            var filter = new TutorReportFilterDto
            {
                TutorId     = tutor.TutorId,
                InstituteId = instituteId,
                NoInstitute = noInstitute,
                ClassId     = classId
            };

            var result = await _reportService.GetTutorMonthlyReportAsync(filter);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/report/monthly/pdf
        /// Downloads a consolidated PDF for a specific month.
        /// month + year are required — PDF is always for one month only.
        /// </summary>
        [HttpGet("monthly/pdf")]
        [ApiPurpose("Download Tutor Monthly Report PDF")]
        public async Task<IActionResult> DownloadMonthlyReportPdf(
            [FromQuery] Guid? instituteId,
            [FromQuery] bool noInstitute = false,
            [FromQuery] Guid? classId = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            if (!month.HasValue || !year.HasValue)
                return BadRequest(new { message = "month and year query parameters are required." });

            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return NotFound(new { message = "Tutor profile not found." });

            var filter = new TutorReportFilterDto
            {
                TutorId     = tutor.TutorId,
                InstituteId = instituteId,
                NoInstitute = noInstitute,
                ClassId     = classId,
                Month       = month,
                Year        = year
            };

            var pdfBytes = await _reportService.GenerateTutorMonthlyReportPdfAsync(filter);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No report data found for the given scope and month." });

            string hash = Math.Abs($"{tutor.TutorId}{year}{month}".GetHashCode())
                              .ToString("X")[..4];
            string reference = $"RPT{year % 100:D2}{month:D2}{hash}";

            return File(pdfBytes, "application/pdf", $"Tutorz_{reference}.pdf");
        }
    }
}
