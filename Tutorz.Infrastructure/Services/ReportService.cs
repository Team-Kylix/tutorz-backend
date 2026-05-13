using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Report;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// Provides tutor-scoped monthly report data and PDF generation.
    /// Designed for cost efficiency: uses at most 3 DB queries per request.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly TutorzDbContext _context;
        private readonly ITutorRepository _tutorRepo;

        private static readonly TimeZoneInfo SriLankaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");

        public ReportService(TutorzDbContext context, ITutorRepository tutorRepo)
        {
            _context = context;
            _tutorRepo = tutorRepo;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GRID DATA
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<ServiceResponse<TutorReportResponseDto>> GetTutorMonthlyReportAsync(
            TutorReportFilterDto filter)
        {
            // ── Build class scope ──────────────────────────────────────────────
            var classIds = await GetScopedClassIdsAsync(filter);
            if (!classIds.Any())
                return Ok(new TutorReportResponseDto());

            // ── Q1: Bulk attendance (only present days) ────────────────────────
            var attendances = await _context.Attendances
                .AsNoTracking()
                .Where(a => classIds.Contains(a.ClassId) && a.IsPresent)
                .Select(a => new { a.StudentId, a.ClassId, a.Date })
                .ToListAsync();

            // ── Q2: Bulk payments (paid only) ──────────────────────────────────
            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Where(p => classIds.Contains(p.ClassId) &&
                            (p.Status == "Paid" || p.Status == "PAID"))
                .Select(p => new { p.StudentId, p.ClassId, p.Month, p.Year, p.AmountPaid })
                .ToListAsync();

            // ── Group attendance by (Month, Year) ─────────────────────────────
            var monthGroups = attendances
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .ToList();

            if (!monthGroups.Any())
                return Ok(new TutorReportResponseDto());

            // ── Build scope description string ────────────────────────────────
            string detailsPeriod = await BuildDetailsPeriodAsync(filter);

            var rows = new List<TutorMonthReportRowDto>();

            foreach (var group in monthGroups)
            {
                int m = group.Key.Month;
                int y = group.Key.Year;

                // Students who attended ≥ 1 day this month
                var studentIds = group.Select(a => a.StudentId).Distinct().ToHashSet();
                int total = studentIds.Count;

                // Count paid students for this month from our already-loaded payments
                int paidCount = payments
                    .Where(p => p.Month == m && p.Year == y && studentIds.Contains(p.StudentId))
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count();

                int unpaid = total - paidCount;

                // Build reference: RPT{YY}{MM}{hash}
                string hash = Math.Abs($"{filter.TutorId}{y}{m}".GetHashCode())
                                  .ToString("X")[..4];
                string reference = $"RPT{y % 100:D2}{m:D2}{hash}";

                rows.Add(new TutorMonthReportRowDto
                {
                    Reference = reference,
                    MonthYear = new DateTime(y, m, 1).ToString("MMMM yyyy"),
                    Month = m,
                    Year = y,
                    DetailsPeriod = detailsPeriod,
                    TotalStudents = total,
                    PaidCount = paidCount,
                    UnpaidCount = unpaid < 0 ? 0 : unpaid
                });
            }

            return Ok(new TutorReportResponseDto { Rows = rows });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PDF GENERATION
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<byte[]?> GenerateTutorMonthlyReportPdfAsync(TutorReportFilterDto filter)
        {
            if (!filter.Month.HasValue || !filter.Year.HasValue)
                return null;

            int month = filter.Month.Value;
            int year  = filter.Year.Value;

            var monthStart = new DateTime(year, month, 1);
            var monthEnd   = monthStart.AddMonths(1); // exclusive upper bound

            // ── Build class scope ──────────────────────────────────────────────
            var classQuery = _context.Classes
                .AsNoTracking()
                .Include(c => c.Institute)
                .Where(c => c.TutorId == filter.TutorId && !c.IsDeleted);

            classQuery = ApplyInstituteFilter(classQuery, filter);
            if (filter.ClassId.HasValue && filter.ClassId.Value != Guid.Empty)
                classQuery = classQuery.Where(c => c.ClassId == filter.ClassId.Value);

            var classes = await classQuery.ToListAsync();
            if (!classes.Any()) return null;

            var classIds = classes.Select(c => c.ClassId).ToList();

            // ── Q1: Attendance for the specific month ─────────────────────────
            var attendances = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => classIds.Contains(a.ClassId)
                         && a.IsPresent
                         && a.Date >= monthStart
                         && a.Date < monthEnd)
                .Select(a => new
                {
                    a.StudentId,
                    a.ClassId,
                    StudentName = a.Student.FirstName + " " + a.Student.LastName,
                    RegNo = a.Student.RegistrationNumber
                })
                .ToListAsync();

            if (!attendances.Any()) return null;

            // ── Q2: Payments for the specific month ───────────────────────────
            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Where(p => classIds.Contains(p.ClassId)
                         && p.Month == month
                         && p.Year == year
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .Select(p => new { p.StudentId, p.ClassId, p.AmountPaid })
                .ToListAsync();

            var paidLookup = payments.ToDictionary(p => (p.StudentId, p.ClassId));

            // ── Group: Institute → Class → Students ───────────────────────────
            // Group classes by institute
            var classByInstitute = classes
                .GroupBy(c => c.InstituteId?.ToString() ?? "own")
                .OrderBy(g => g.Key == "own" ? "ZZZZ" : g.First().Institute?.InstituteName ?? "")
                .ToList();

            // Build PDF sections
            var sections = new List<TutorReportClassSectionDto>();
            foreach (var instGroup in classByInstitute)
            {
                foreach (var cls in instGroup.OrderBy(c => c.ClassName ?? c.Subject))
                {
                    // Students who attended this class this month
                    var classAttendees = attendances
                        .Where(a => a.ClassId == cls.ClassId)
                        .GroupBy(a => a.StudentId)
                        .ToList();

                    if (!classAttendees.Any()) continue;

                    var students = classAttendees.Select(g =>
                    {
                        var first = g.First();
                        paidLookup.TryGetValue((g.Key, cls.ClassId), out var pay);
                        return new TutorReportStudentDetailDto
                        {
                            StudentName      = first.StudentName.Trim(),
                            RegistrationNumber = first.RegNo,
                            AttendanceCount  = g.Count(),
                            PaymentStatus    = pay != null ? "Paid" : "Not Yet",
                            PaidAmount       = pay?.AmountPaid
                        };
                    }).OrderBy(s => s.StudentName).ToList();

                    sections.Add(new TutorReportClassSectionDto
                    {
                        ClassName     = cls.ClassName ?? cls.Subject ?? "Class",
                        InstituteName = cls.InstituteId.HasValue
                            ? (cls.Institute?.InstituteName ?? "Institute")
                            : "My Own Place",
                        Students = students
                    });
                }
            }

            if (!sections.Any()) return null;

            // ── PDF Header info ───────────────────────────────────────────────
            string periodStr = new DateTime(year, month, 1).ToString("MMMM yyyy");
            string scopeStr  = await BuildDetailsPeriodAsync(filter);
            string hash      = Math.Abs($"{filter.TutorId}{year}{month}".GetHashCode())
                                   .ToString("X")[..4];
            string reference = $"RPT{year % 100:D2}{month:D2}{hash}";

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo  = File.Exists(logoPath);

            // ── Build QuestPDF document ───────────────────────────────────────
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(45);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ── HEADER (mirrors StudentBillService) ───────────────────
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(40).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk")
                                    .FontSize(18).Bold().FontColor(Colors.Blue.Medium);

                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("MONTHLY CLASS REPORT").FontSize(16).Bold();
                            col.Item().Text($"Ref #: {reference}");
                            col.Item().Text($"Date:  {DateTime.UtcNow:dd MMM yyyy}");
                            col.Item().Text($"Period: {periodStr}").Bold();
                            col.Item().Text($"Scope: {scopeStr}").FontSize(8).Italic();
                        });
                    });

                    // ── CONTENT ───────────────────────────────────────────────
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(10)
                            .BorderBottom(1).BorderColor(Colors.Grey.Medium);

                        string currentInstitute = null!;

                        foreach (var section in sections)
                        {
                            // Institute section header (only when it changes)
                            if (section.InstituteName != currentInstitute)
                            {
                                currentInstitute = section.InstituteName;
                                col.Item().PaddingTop(10).Text(
                                    $"INSTITUTE: {currentInstitute}")
                                    .FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                            }

                            // Class sub-header
                            col.Item().PaddingTop(6).PaddingLeft(12).Text(
                                $"Class: {section.ClassName}")
                                .FontSize(10).Bold();

                            // Student table
                            col.Item().PaddingTop(4).PaddingLeft(12).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(20);   // #
                                    cols.RelativeColumn(3);    // Student Name
                                    cols.RelativeColumn(2);    // Reg No
                                    cols.ConstantColumn(65);   // Attendance
                                    cols.RelativeColumn(2);    // Payment Status
                                    cols.ConstantColumn(80);   // Amount (LKR)
                                });

                                // Table header
                                table.Header(header =>
                                {
                                    header.Cell().Text("#").Bold();
                                    header.Cell().Text("Student Name").Bold();
                                    header.Cell().Text("Reg No").Bold();
                                    header.Cell().AlignCenter().Text("Attendance").Bold();
                                    header.Cell().Text("Payment Status").Bold();
                                    header.Cell().AlignRight().Text("Amount (LKR)").Bold();

                                    header.Cell().ColumnSpan(6).PaddingVertical(3)
                                        .BorderBottom(1).BorderColor(Colors.Grey.Medium);
                                });

                                int rowNum = 1;
                                foreach (var s in section.Students)
                                {
                                    bool isPaid = s.PaymentStatus == "Paid";
                                    var statusColor = isPaid ? Colors.Green.Darken2 : Colors.Red.Medium;

                                    table.Cell().PaddingVertical(3).Text($"{rowNum++}");
                                    table.Cell().PaddingVertical(3).Text(s.StudentName);
                                    table.Cell().PaddingVertical(3).Text(s.RegistrationNumber ?? "—");
                                    table.Cell().PaddingVertical(3).AlignCenter()
                                        .Text($"{s.AttendanceCount} day{(s.AttendanceCount != 1 ? "s" : "")}");
                                    table.Cell().PaddingVertical(3).Text(text =>
                                    {
                                        text.Span(s.PaymentStatus).FontColor(statusColor).Bold();
                                    });
                                    table.Cell().PaddingVertical(3).AlignRight().Text(
                                        s.PaidAmount.HasValue ? $"{s.PaidAmount.Value:N2}" : "–");
                                }

                                // Totals row
                                table.Footer(footer =>
                                {
                                    footer.Cell().ColumnSpan(6).PaddingVertical(3)
                                        .BorderTop(1).BorderColor(Colors.Grey.Medium);

                                    int paid   = section.Students.Count(x => x.PaymentStatus == "Paid");
                                    int notYet = section.Students.Count - paid;

                                    footer.Cell().ColumnSpan(4).AlignRight()
                                        .Text($"Total: {section.Students.Count} student{(section.Students.Count != 1 ? "s" : "")}   |   Paid: {paid}   |   Not Yet: {notYet}")
                                        .FontSize(8).Italic();

                                    decimal totalPaid = section.Students
                                        .Where(x => x.PaidAmount.HasValue)
                                        .Sum(x => x.PaidAmount!.Value);
                                    footer.Cell().ColumnSpan(2).AlignRight()
                                        .Text(totalPaid > 0 ? $"{totalPaid:N2}" : "–")
                                        .Bold();
                                });
                            });

                            col.Item().PaddingTop(6)
                                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                        }

                        // Footer note
                        col.Item().PaddingTop(20)
                            .Text("Note: This is a system-generated report. Attendance counts reflect records from 1st to last day of the reported month.")
                            .Italic().FontSize(7).FontColor(Colors.Grey.Medium);
                    });

                    // ── PAGE FOOTER ───────────────────────────────────────────
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<List<Guid>> GetScopedClassIdsAsync(TutorReportFilterDto filter)
        {
            var query = _context.Classes
                .AsNoTracking()
                .Where(c => c.TutorId == filter.TutorId && !c.IsDeleted);

            query = ApplyInstituteFilter(query, filter);

            if (filter.ClassId.HasValue && filter.ClassId.Value != Guid.Empty)
                query = query.Where(c => c.ClassId == filter.ClassId.Value);

            return await query.Select(c => c.ClassId).ToListAsync();
        }

        private IQueryable<Domain.Entities.Class> ApplyInstituteFilter(
            IQueryable<Domain.Entities.Class> query, TutorReportFilterDto filter)
        {
            if (filter.ClassId.HasValue && filter.ClassId.Value != Guid.Empty)
                return query; // class filter already narrows enough

            if (filter.NoInstitute)
                return query.Where(c => c.InstituteId == null);

            if (filter.InstituteId.HasValue && filter.InstituteId.Value != Guid.Empty)
                return query.Where(c => c.InstituteId == filter.InstituteId.Value);

            return query; // all
        }

        private async Task<string> BuildDetailsPeriodAsync(TutorReportFilterDto filter)
        {
            string institutePart = "All Institutes";
            if (filter.NoInstitute)
                institutePart = "My Own Place";
            else if (filter.InstituteId.HasValue && filter.InstituteId.Value != Guid.Empty)
            {
                var inst = await _context.Institutes.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.InstituteId == filter.InstituteId.Value);
                institutePart = inst?.InstituteName ?? "Institute";
            }

            string classPart = "All Classes";
            if (filter.ClassId.HasValue && filter.ClassId.Value != Guid.Empty)
            {
                var cls = await _context.Classes.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClassId == filter.ClassId.Value);
                classPart = cls?.ClassName ?? cls?.Subject ?? "Class";
            }

            return $"{institutePart} · {classPart}";
        }

        private static ServiceResponse<TutorReportResponseDto> Ok(TutorReportResponseDto data) =>
            new() { Success = true, Data = data };
    }
}
