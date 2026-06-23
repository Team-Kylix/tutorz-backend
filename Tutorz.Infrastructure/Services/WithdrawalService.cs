using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tutorz.Application.DTOs.Withdrawal;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Common;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly TutorzDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IIdGeneratorService _idGenerator;

        public WithdrawalService(
            TutorzDbContext context,
            INotificationService notificationService,
            IIdGeneratorService idGenerator)
        {
            _context = context;
            _notificationService = notificationService;
            _idGenerator = idGenerator;
        }

        public async Task<ServiceResponse<decimal>> GetAvailableBalanceAsync(Guid tutorId, Guid instituteId)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null) return ServiceResponse<decimal>.ErrorResponse("Tutor not found.");

            var institute = await _context.Institutes.FindAsync(instituteId);
            if (institute == null) return ServiceResponse<decimal>.ErrorResponse("Institute not found.");

            var query = _context.ClassPayments
                .Include(p => p.Class)
                .Where(p => p.Class.TutorId == tutorId && p.InstituteId == instituteId);
            
            decimal totalTuitionAccrued = await query.SumAsync(p => p.TuitionAmount ?? 0);
            
            decimal totalWithdrawn = await _context.Withdrawals
                .Where(w => w.TutorId == tutorId && w.InstituteId == instituteId)
                .SumAsync(w => w.WithdrawalAmount);

            return ServiceResponse<decimal>.SuccessResponse(totalTuitionAccrued - totalWithdrawn);
        }

        public async Task<ServiceResponse<IEnumerable<WithdrawalDto>>> GetTutorWithdrawalsAsync(Guid tutorId, Guid? instituteId)
        {
            var query = _context.Withdrawals
                .Include(w => w.Institute)
                .Include(w => w.Tutor)
                .ThenInclude(t => t.User)
                .Where(w => w.TutorId == tutorId);

            if (instituteId.HasValue)
                query = query.Where(w => w.InstituteId == instituteId.Value);

            var list = await query.OrderByDescending(w => w.WithdrawalAt).ToListAsync();
            return ServiceResponse<IEnumerable<WithdrawalDto>>.SuccessResponse(list.Select(MapToDto));
        }

        public async Task<ServiceResponse<IEnumerable<WithdrawalDto>>> GetInstituteWithdrawalsAsync(Guid instituteId, Guid? tutorId)
        {
            var query = _context.Withdrawals
                .Include(w => w.Institute)
                .Include(w => w.Tutor)
                .ThenInclude(t => t.User)
                .Where(w => w.InstituteId == instituteId);

            if (tutorId.HasValue)
                query = query.Where(w => w.TutorId == tutorId.Value);

            var list = await query.OrderByDescending(w => w.WithdrawalAt).ToListAsync();
            return ServiceResponse<IEnumerable<WithdrawalDto>>.SuccessResponse(list.Select(MapToDto));
        }

        public async Task<ServiceResponse<bool>> NotifyInstituteForWithdrawalAsync(Guid tutorId, WithdrawalRequestDto request)
        {
            var tutor = await _context.Tutors.Include(t => t.User).FirstOrDefaultAsync(t => t.TutorId == tutorId);
            if (tutor == null) return ServiceResponse<bool>.ErrorResponse("Tutor not found.");

            var institute = await _context.Institutes
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InstituteId == request.InstituteId);

            if (institute == null) return ServiceResponse<bool>.ErrorResponse("Institute not found.");

            var balanceResult = await GetAvailableBalanceAsync(tutorId, request.InstituteId);
            if (!balanceResult.Success) return ServiceResponse<bool>.ErrorResponse(balanceResult.Message);

            if (request.RequestedAmount > balanceResult.Data)
            {
                return ServiceResponse<bool>.ErrorResponse("Requested amount exceeds available balance.");
            }

            string tutorName = tutor.FirstName + " " + tutor.LastName;

            await _notificationService.CreateAndPushAsync(
                institute.UserId,
                "Withdrawal Request",
                $"{tutorName} has requested a withdrawal of Rs {request.RequestedAmount:N2}.",
                "WithdrawalRequest",
                tutorId);

            return ServiceResponse<bool>.SuccessResponse(true);
        }

        public async Task<ServiceResponse<WithdrawalDto>> ProcessWithdrawalAsync(Guid instituteId, WithdrawalProcessDto dto)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorId == dto.TutorId);
                
            if (tutor == null) return ServiceResponse<WithdrawalDto>.ErrorResponse("Tutor not found.");

            var balanceResult = await GetAvailableBalanceAsync(dto.TutorId, instituteId);
            if (!balanceResult.Success) return ServiceResponse<WithdrawalDto>.ErrorResponse(balanceResult.Message);

            var currentBalance = balanceResult.Data;
            if (dto.WithdrawalAmount > currentBalance)
                return ServiceResponse<WithdrawalDto>.ErrorResponse("Withdrawal amount exceeds available balance.");

            var lastWithdrawal = await _context.Withdrawals
                .Where(w => w.TutorId == dto.TutorId && w.InstituteId == instituteId)
                .OrderByDescending(w => w.WithdrawalAt)
                .FirstOrDefaultAsync();

            DateTime periodStart = lastWithdrawal?.PeriodEnd ?? new DateTime(2025, 1, 1);
            DateTime periodEnd = DateTime.UtcNow;

            string referenceId = await _idGenerator.GenerateWithdrawalReferenceAsync();
            decimal payoutCommission = 0;
            
            if (dto.PaymentMethod == "Online")
            {
                payoutCommission = Math.Round(dto.WithdrawalAmount * 0.03m, 2); 
            }

            var withdrawal = new Withdrawal
            {
                ReferenceId = referenceId,
                InstituteId = instituteId,
                TutorId = dto.TutorId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CurrentBalance = currentBalance,
                WithdrawalAmount = dto.WithdrawalAmount,
                RemainingBalance = currentBalance - dto.WithdrawalAmount,
                PaymentMethod = dto.PaymentMethod,
                PayoutCommission = dto.PaymentMethod == "Online" ? payoutCommission : 0,
                WithdrawalAt = DateTime.UtcNow
            };

            _context.Withdrawals.Add(withdrawal);
            await _context.SaveChangesAsync();

            await _notificationService.CreateAndPushAsync(
                tutor.UserId,
                "Withdrawal Processed",
                $"An amount of Rs {dto.WithdrawalAmount:N2} was withdrawn by the institute ({dto.PaymentMethod}).",
                "WithdrawalProcessed",
                withdrawal.WithdrawalId);

            var savedEntity = await _context.Withdrawals
                .Include(w => w.Institute)
                .Include(w => w.Tutor)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawal.WithdrawalId);

            return ServiceResponse<WithdrawalDto>.SuccessResponse(MapToDto(savedEntity!));
        }

        private WithdrawalDto MapToDto(Withdrawal w)
        {
            string tutorName = w.Tutor != null ? w.Tutor.FirstName + " " + w.Tutor.LastName : string.Empty;

            return new WithdrawalDto
            {
                WithdrawalId = w.WithdrawalId,
                ReferenceId = w.ReferenceId,
                InstituteId = w.InstituteId,
                InstituteName = w.Institute?.InstituteName ?? string.Empty,
                TutorId = w.TutorId,
                TutorName = tutorName,
                ClassId = w.ClassId,
                PeriodStart = w.PeriodStart,
                PeriodEnd = w.PeriodEnd,
                CurrentBalance = w.CurrentBalance,
                WithdrawalAmount = w.WithdrawalAmount,
                RemainingBalance = w.RemainingBalance,
                PaymentMethod = w.PaymentMethod,
                PayoutCommission = w.PayoutCommission,
                WithdrawalAt = w.WithdrawalAt
            };
        }

        // â”€â”€â”€ Shared PDF helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string Fmt(decimal v) => $"Rs {v:N2}";

        private record ClassTotals(decimal Gross, decimal Commission, decimal Pending);

        /// <summary>
        /// Renders a class-summary table into a QuestPDF container and returns totals.
        /// Matches the BillService/ReportService visual style.
        /// </summary>
        private static ClassTotals RenderClassSummaryRows(
            IContainer container,
            IEnumerable<ClassPayment> payments)
        {
            var byClass = payments
                .GroupBy(p => p.ClassId)
                .Select(g =>
                {
                    var cls        = g.First().Class;
                    string cName   = cls?.ClassName ?? cls?.Subject ?? "Class";
                    string day     = cls?.DayOfWeek ?? string.Empty;
                    string label   = string.IsNullOrWhiteSpace(day) ? cName : $"{cName} ({day})";

                    var online  = g.Where(p => !string.IsNullOrEmpty(p.PayHerePaymentId) || p.PaymentMethod == "CARD" || p.PaymentMethod == "VISA").ToList();
                    var onhand  = g.Where(p =>  string.IsNullOrEmpty(p.PayHerePaymentId) && p.PaymentMethod != "CARD" && p.PaymentMethod != "VISA").ToList();

                    decimal clsGross      = g.Sum(p => p.AmountPaid);
                    decimal clsCommission = g.Sum(p => p.InstituteAmount ?? 0);
                    decimal clsPending    = g.Sum(p => p.TuitionAmount  ?? 0);
                    decimal onlineGross   = online.Sum(p => p.AmountPaid);
                    decimal onhandGross   = onhand.Sum(p => p.AmountPaid);
                    decimal rate          = g.First().InstituteCommissionPercentage ?? 0;

                    return new { label, g = g.ToList(), online, onhand, clsGross, clsCommission, clsPending, onlineGross, onhandGross, rate };
                })
                .OrderBy(x => x.label)
                .ToList();

            decimal totalGross      = byClass.Sum(c => c.clsGross);
            decimal totalCommission = byClass.Sum(c => c.clsCommission);
            decimal totalPending    = byClass.Sum(c => c.clsPending);

            container.Table(table =>
            {
                // Columns: Class Name | Students (Online/Hand) | Gross | Commission | Net
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3);   // Class
                    cd.ConstantColumn(55);  // Online
                    cd.ConstantColumn(55);  // On-Hand
                    cd.ConstantColumn(85);  // Gross
                    cd.ConstantColumn(85);  // Commission
                    cd.ConstantColumn(85);  // Net
                });

                // â”€â”€ Table header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                table.Header(h =>
                {
                    void Hdr(string t, bool right = false)
                    {
                        var cell = h.Cell().PaddingVertical(4);
                        if (right) cell.AlignRight().Text(t).Bold().FontSize(8.5f);
                        else       cell.Text(t).Bold().FontSize(8.5f);
                    }
                    Hdr("Class");
                    Hdr("Online",  right: true);
                    Hdr("On-Hand", right: true);
                    Hdr("Gross (Rs)",      right: true);
                    Hdr("Commission (Rs)", right: true);
                    Hdr("Net (Rs)",        right: true);

                    h.Cell().ColumnSpan(6).PaddingVertical(3)
                        .BorderBottom(1).BorderColor(Colors.Black);
                });

                // â”€â”€ Data rows â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                foreach (var c in byClass)
                {
                    void DCell(string val, bool bold = false, bool right = false, string? color = null)
                    {
                        var cell = table.Cell().PaddingVertical(3);
                        var txt  = right ? cell.AlignRight().Text(val).FontSize(9)
                                         : cell.Text(val).FontSize(9);
                        if (bold) txt.Bold();
                        if (color != null) txt.FontColor(color);
                    }

                    DCell($"{c.label}  ({c.g.Count} student{(c.g.Count > 1 ? "s" : "")})"  );
                    DCell($"{c.online.Count}", right: true, color: c.online.Count > 0 ? "0369A1" : "9CA3AF");
                    DCell($"{c.onhand.Count}", right: true, color: c.onhand.Count > 0 ? "374151" : "9CA3AF");
                    DCell($"{c.clsGross:N2}",      bold: true,  right: true);
                    DCell($"{c.clsCommission:N2}",              right: true, color: "B91C1C");
                    DCell($"{c.clsPending:N2}",    bold: true,  right: true, color: "15803D");

                    // Separator
                    table.Cell().ColumnSpan(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                }

                // â”€â”€ Footer totals row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                table.Footer(f =>
                {
                    f.Cell().ColumnSpan(6).PaddingVertical(3)
                        .BorderTop(1).BorderColor(Colors.Black);

                    void FCell(string val, bool bold = false, bool right = false, string? color = null)
                    {
                        var cell = f.Cell().PaddingVertical(2);
                        var txt  = right ? cell.AlignRight().Text(val).FontSize(9)
                                         : cell.Text(val).FontSize(9);
                        if (bold) txt.Bold();
                        if (color != null) txt.FontColor(color);
                    }

                    FCell("Total", bold: true);
                    FCell("", right: true);
                    FCell("", right: true);
                    FCell($"{totalGross:N2}",      bold: true, right: true);
                    FCell($"{totalCommission:N2}",             right: true, color: "B91C1C");
                    FCell($"{totalPending:N2}",    bold: true, right: true, color: "15803D");
                });
            });

            return new ClassTotals(totalGross, totalCommission, totalPending);
        }

        // â”€â”€â”€ 1. Tutor withdrawal receipt PDF â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<byte[]> GenerateWithdrawalPdfAsync(Guid withdrawalId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var withdrawal = await _context.Withdrawals
                .Include(w => w.Institute)
                .Include(w => w.Tutor)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId);

            if (withdrawal == null) return Array.Empty<byte>();

            var payments = await _context.ClassPayments
                .Include(p => p.Class)
                .Include(p => p.Student)
                .Where(p => p.Class.TutorId == withdrawal.TutorId 
                         && p.InstituteId == withdrawal.InstituteId
                         && p.PaidAt >= withdrawal.PeriodStart 
                         && p.PaidAt <= withdrawal.PeriodEnd
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .ToListAsync();

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // â”€â”€ WITHDRAWN watermark â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Foreground()
                        .AlignCenter().AlignMiddle()
                        .Width(600).Rotate(-45)
                        .Text("WITHDRAWN")
                        .FontSize(72).Bold().FontColor("#1A000000");

                    // â”€â”€ Header (matches PLATFORM INVOICE style) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("WITHDRAWAL RECEIPT").FontSize(20).Bold();
                            col.Item().Text($"Ref #: {withdrawal.ReferenceId}");
                            col.Item().Text($"Date: {withdrawal.WithdrawalAt:dd MMM yyyy}");
                        });
                    });

                    // â”€â”€ Content â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        string tName    = withdrawal.Tutor != null ? $"{withdrawal.Tutor.FirstName} {withdrawal.Tutor.LastName}" : "Unknown";
                        string instName = withdrawal.Institute?.InstituteName ?? "Unknown";

                        // Issued To / Period block
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Issued To:").Bold();
                                c.Item().Text(tName);
                                if (!string.IsNullOrWhiteSpace(withdrawal.Tutor?.User?.Email))
                                    c.Item().Text(withdrawal.Tutor.User.Email);
                                c.Item().Text($"Institute: {instName}");
                                c.Item().Text("Role: Tutor");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Period:").Bold();
                                c.Item().Text($"{withdrawal.PeriodStart:dd MMM yyyy} - {withdrawal.PeriodEnd:dd MMM yyyy}");
                                c.Item().Text($"Payment Method: {withdrawal.PaymentMethod}");
                            });
                        });

                        // Class-wise earnings table
                        col.Item().PaddingTop(20).Text("CLASS-WISE EARNINGS SUMMARY").Bold();

                        ClassTotals totals = default!;
                        col.Item().PaddingTop(5).Element(e => { totals = RenderClassSummaryRows(e, payments); });
                        decimal totalGross      = totals?.Gross ?? 0;
                        decimal totalCommission = totals?.Commission ?? 0;
                        decimal totalPending    = totals?.Pending ?? 0;

                        // Settlement summary table
                        col.Item().PaddingTop(20).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(130); });

                            void SR(string lbl, string val, bool bold = false)
                            {
                                var l = t.Cell().PaddingVertical(2).Text(lbl); if (bold) l.Bold();
                                var r = t.Cell().AlignRight().PaddingVertical(2).Text(val); if (bold) r.Bold();
                            }

                            t.Header(h =>
                            {
                                h.Cell().ColumnSpan(2).PaddingVertical(3)
                                    .Text("WITHDRAWAL SETTLEMENT").Bold();
                                h.Cell().ColumnSpan(2).PaddingVertical(3)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            SR("Available Balance Before Withdrawal", $"{withdrawal.CurrentBalance:N2}");
                            SR($"Withdrawal Amount ({withdrawal.PaymentMethod})", $"{withdrawal.WithdrawalAmount:N2}", bold: true);

                            if (withdrawal.PaymentMethod == "Online" && (withdrawal.PayoutCommission ?? 0) > 0)
                            {
                                SR("Online Payout Fee (3%)", $"- {withdrawal.PayoutCommission!.Value:N2}");
                                SR("Net Amount Transferred", $"{(withdrawal.WithdrawalAmount - withdrawal.PayoutCommission!.Value):N2}", bold: true);
                            }

                            t.Footer(f =>
                            {
                                f.Cell().ColumnSpan(2).PaddingVertical(3)
                                    .BorderTop(1).BorderColor(Colors.Black);
                                f.Cell().PaddingVertical(2).Text("Remaining Balance at Institute").Bold().FontSize(12);
                                f.Cell().AlignRight().PaddingVertical(2).Text($"{withdrawal.RemainingBalance:N2}").Bold().FontSize(12);
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span("WITHDRAWN").Bold().FontColor(Colors.Green.Medium);
                            });
                            c.Item().PaddingTop(10)
                                .Text("Note: This is a system-generated withdrawal receipt. Please retain for your records.")
                                .Italic().FontSize(8);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  OVERVIEW  (one row per pairing, even if no withdrawal yet)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>> GetTutorWithdrawalOverviewAsync(
            Guid tutorId, Guid? instituteId)
        {
            // â”€â”€ 1. Gather classes this tutor teaches â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var classQuery = _context.Classes
                .AsNoTracking()
                .Include(c => c.Institute)
                .Where(c => c.TutorId == tutorId && !c.IsDeleted && c.InstituteId != null);

            if (instituteId.HasValue && instituteId.Value != Guid.Empty)
                classQuery = classQuery.Where(c => c.InstituteId == instituteId.Value);

            var classes = await classQuery.ToListAsync();

            bool combineAll = !instituteId.HasValue;
            var groups = combineAll
                ? classes.GroupBy(c => (Guid?)null)
                : classes.GroupBy(c => (Guid?)c.InstituteId!.Value);

            var classIds = classes.Select(c => c.ClassId).ToList();
            if (!classIds.Any())
                return ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>.SuccessResponse(
                    Array.Empty<WithdrawalOverviewRowDto>());

            // â”€â”€ 2. Bulk-load payments & withdrawals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Include(p => p.Class)
                .Where(p => classIds.Contains(p.ClassId)
                         && p.InstituteId != null
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .Select(p => new { p.ClassId, p.InstituteId, p.TuitionAmount, p.PaidAt })
                .ToListAsync();

            var withdrawals = await _context.Withdrawals
                .AsNoTracking()
                .Where(w => w.TutorId == tutorId)
                .OrderByDescending(w => w.WithdrawalAt)
                .Select(w => new { w.WithdrawalId, w.InstituteId, w.ClassId, w.ReferenceId,
                                   w.WithdrawalAmount, w.CurrentBalance, w.PaymentMethod, w.PeriodEnd, w.WithdrawalAt })
                .ToListAsync();

            var rows = new List<WithdrawalOverviewRowDto>();
            DateTime now = DateTime.UtcNow;

            foreach (var g in groups)
            {
                var gInstId = g.Key;

                // payments in scope
                var scopePayments = combineAll
                    ? payments.ToList()
                    : payments.Where(p => p.InstituteId == gInstId).ToList();

                decimal totalAccrued = scopePayments.Sum(p => p.TuitionAmount ?? 0);
                if (totalAccrued == 0) continue; // no money ever paid, skip

                // withdrawals in scope
                var scopeWithdrawals = combineAll
                    ? withdrawals.ToList()
                    : withdrawals.Where(w => w.InstituteId == gInstId).ToList();

                decimal totalWithdrawn = scopeWithdrawals.Sum(w => w.WithdrawalAmount);
                decimal availBal = totalAccrued - totalWithdrawn;

                // Details period: Institute name
                var firstClass = g.First();
                string instituteName = combineAll ? "All Institutes" : (firstClass.Institute?.InstituteName ?? "Institute");
                string detailsPeriod = combineAll ? "All Institutes" : instituteName;

                // 1. Pending Row (Current available balance)
                // Always show if availBal >= 0 so tutors know their current pending balance
                if (availBal >= 0)
                {
                    var lastWForPending = scopeWithdrawals.OrderByDescending(w => w.WithdrawalAt).FirstOrDefault();
                    DateTime pendingPeriodStart = lastWForPending != null
                        ? lastWForPending.PeriodEnd
                        : (scopePayments.Any() ? scopePayments.Min(p => p.PaidAt) : DateTime.UtcNow);

                    rows.Add(new WithdrawalOverviewRowDto
                    {
                        ReferenceId      = "PENDING",
                        DetailsPeriod    = detailsPeriod,
                        WithdrawalPeriod = $"{pendingPeriodStart:dd MMM} - {now:dd MMM yyyy}",
                        PeriodStart      = pendingPeriodStart,
                        PeriodEnd        = now,
                        AvailableBalance = availBal,
                        WithdrawalAmount = null,
                        PaymentMethod    = null,
                        LastWithdrawalId = null,
                        InstituteId      = gInstId,
                        InstituteName    = instituteName,
                        WithdrawalAt     = null,
                        IsPendingRow     = true
                    });
                }

                // 2. Historical Rows
                var sortedW = scopeWithdrawals.OrderBy(w => w.WithdrawalAt).ToList();
                DateTime prevEnd = scopePayments.Any() ? scopePayments.Min(p => p.PaidAt) : DateTime.UtcNow;

                foreach (var w in sortedW)
                {
                    string periodStr = $"{prevEnd:dd MMM} - {w.PeriodEnd:dd MMM yyyy}";
                    rows.Add(new WithdrawalOverviewRowDto
                    {
                        ReferenceId      = w.ReferenceId,
                        DetailsPeriod    = detailsPeriod,
                        WithdrawalPeriod = periodStr,
                        PeriodStart      = prevEnd,
                        PeriodEnd        = w.PeriodEnd,
                        AvailableBalance = w.CurrentBalance, // Show the available balance at the time of withdrawal
                        WithdrawalAmount = w.WithdrawalAmount,
                        PaymentMethod    = w.PaymentMethod,
                        LastWithdrawalId = w.WithdrawalId,
                        InstituteId      = gInstId,
                        InstituteName    = instituteName,
                        WithdrawalAt     = w.WithdrawalAt,
                        IsPendingRow     = false
                    });
                    prevEnd = w.PeriodEnd;
                }
            }

            // Return sorting: Pending first, then by date descending
            return ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>.SuccessResponse(
                rows.OrderBy(r => r.InstituteName)
                    .ThenByDescending(r => r.IsPendingRow)
                    .ThenByDescending(r => r.WithdrawalAt ?? DateTime.MaxValue));
        }

        public async Task<ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>> GetInstituteWithdrawalOverviewAsync(
            Guid instituteId, Guid? tutorId)
        {
            // â”€â”€ 1. Gather classes at this institute â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var classQuery = _context.Classes
                .AsNoTracking()
                .Include(c => c.Tutor)
                .Where(c => c.InstituteId == instituteId && !c.IsDeleted);

            if (tutorId.HasValue && tutorId.Value != Guid.Empty)
                classQuery = classQuery.Where(c => c.TutorId == tutorId.Value);

            var classes = await classQuery.ToListAsync();

            bool combineAll = !tutorId.HasValue;
            var groups = combineAll
                ? classes.GroupBy(c => (Guid?)null)
                : classes.GroupBy(c => (Guid?)c.TutorId);

            var classIds = classes.Select(c => c.ClassId).ToList();
            if (!classIds.Any())
                return ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>.SuccessResponse(
                    Array.Empty<WithdrawalOverviewRowDto>());

            // â”€â”€ 2. Bulk-load payments & withdrawals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Include(p => p.Class)
                .Where(p => classIds.Contains(p.ClassId)
                         && p.InstituteId == instituteId
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .Select(p => new { p.ClassId, TutorId = p.Class.TutorId, p.TuitionAmount, p.PaidAt })
                .ToListAsync();

            var withdrawals = await _context.Withdrawals
                .AsNoTracking()
                .Include(w => w.Tutor)
                .Where(w => w.InstituteId == instituteId)
                .OrderByDescending(w => w.WithdrawalAt)
                .Select(w => new { w.WithdrawalId, w.TutorId,
                                   TutorFirstName = w.Tutor.FirstName,
                                   TutorLastName  = w.Tutor.LastName,
                                   w.ClassId, w.ReferenceId,
                                   w.WithdrawalAmount, w.CurrentBalance, w.PaymentMethod, w.PeriodEnd, w.WithdrawalAt })
                .ToListAsync();

            var rows = new List<WithdrawalOverviewRowDto>();
            DateTime now = DateTime.UtcNow;

            foreach (var g in groups)
            {
                var gTutorId = g.Key;

                var scopePayments = combineAll
                    ? payments.ToList()
                    : payments.Where(p => p.TutorId == gTutorId).ToList();

                decimal totalAccrued = scopePayments.Sum(p => p.TuitionAmount ?? 0);
                if (totalAccrued == 0) continue;

                var scopeWithdrawals = combineAll
                    ? withdrawals.ToList()
                    : withdrawals.Where(w => w.TutorId == gTutorId).ToList();

                decimal totalWithdrawn = scopeWithdrawals.Sum(w => w.WithdrawalAmount);
                decimal availBal = totalAccrued - totalWithdrawn;

                var firstClass  = g.First();
                var tutor       = firstClass.Tutor;
                string tutorName = combineAll ? "All Tutors" : (tutor != null
                    ? $"{tutor.FirstName} {tutor.LastName}".Trim()
                    : "Unknown");
                string detailsPeriod = tutorName;

                // 1. Pending Row
                if (availBal >= 0)
                {
                    var lastWForPending = scopeWithdrawals.OrderByDescending(w => w.WithdrawalAt).FirstOrDefault();
                    DateTime pendingPeriodStart = lastWForPending != null
                        ? lastWForPending.PeriodEnd
                        : (scopePayments.Any() ? scopePayments.Min(p => p.PaidAt) : DateTime.UtcNow);

                    rows.Add(new WithdrawalOverviewRowDto
                    {
                        ReferenceId      = "PENDING",
                        DetailsPeriod    = detailsPeriod,
                        WithdrawalPeriod = $"{pendingPeriodStart:dd MMM} - {now:dd MMM yyyy}",
                        PeriodStart      = pendingPeriodStart,
                        PeriodEnd        = now,
                        AvailableBalance = availBal,
                        WithdrawalAmount = null,
                        PaymentMethod    = null,
                        LastWithdrawalId = null,
                        TutorId          = gTutorId,
                        TutorName        = tutorName,
                        WithdrawalAt     = null,
                        IsPendingRow     = true
                    });
                }

                // 2. Historical Rows
                var sortedW = scopeWithdrawals.OrderBy(w => w.WithdrawalAt).ToList();
                DateTime prevEnd = scopePayments.Any() ? scopePayments.Min(p => p.PaidAt) : DateTime.UtcNow;

                foreach (var w in sortedW)
                {
                    string periodStr = $"{prevEnd:dd MMM} - {w.PeriodEnd:dd MMM yyyy}";
                    // For historical rows, always use the actual tutor name from the withdrawal record
                    string histTutorName = $"{w.TutorFirstName} {w.TutorLastName}".Trim();
                    if (string.IsNullOrWhiteSpace(histTutorName)) histTutorName = tutorName;

                    rows.Add(new WithdrawalOverviewRowDto
                    {
                        ReferenceId      = w.ReferenceId,
                        DetailsPeriod    = histTutorName,
                        WithdrawalPeriod = periodStr,
                        PeriodStart      = prevEnd,
                        PeriodEnd        = w.PeriodEnd,
                        AvailableBalance = w.CurrentBalance,
                        WithdrawalAmount = w.WithdrawalAmount,
                        PaymentMethod    = w.PaymentMethod,
                        LastWithdrawalId = w.WithdrawalId,
                        TutorId          = w.TutorId,
                        TutorName        = histTutorName,
                        WithdrawalAt     = w.WithdrawalAt,
                        IsPendingRow     = false
                    });
                    prevEnd = w.PeriodEnd;
                }
            }

            // Return sorting: Pending first, then by date descending
            return ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>.SuccessResponse(
                rows.OrderBy(r => r.TutorName)
                    .ThenByDescending(r => r.IsPendingRow)
                    .ThenByDescending(r => r.WithdrawalAt ?? DateTime.MaxValue));
        }
        // â”€â”€â”€ 2. Tutor pending earnings overview PDF â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<byte[]> GeneratePendingEarningsPdfAsync(Guid tutorId, Guid? instituteId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var tutor = await _context.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId);
            if (tutor == null) return Array.Empty<byte>();

            // Resolve class ids in scope
            var classQuery = _context.Classes
                .Where(c => c.TutorId == tutorId && !c.IsDeleted && c.InstituteId != null);
            if (instituteId.HasValue && instituteId.Value != Guid.Empty)
                classQuery = classQuery.Where(c => c.InstituteId == instituteId.Value);

            var classIds = await classQuery.Select(c => c.ClassId).ToListAsync();
            if (!classIds.Any()) return Array.Empty<byte>();


            var allPayments = await _context.ClassPayments
                .Include(p => p.Class).ThenInclude(c => c.Institute)
                .Where(p => classIds.Contains(p.ClassId) && p.InstituteId != null
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .ToListAsync();

            // â”€â”€ Amount-based balance (totalTuition minus totalWithdrawn) â”€â”€
            decimal totalTuitionAllTime = allPayments.Sum(p => p.TuitionAmount ?? 0);

            decimal totalWithdrawnAllTime = await _context.Withdrawals
                .Where(w => w.TutorId == tutorId
                         && (!instituteId.HasValue || w.InstituteId == instituteId.Value))
                .SumAsync(w => w.WithdrawalAmount);

            decimal pendingBalance = totalTuitionAllTime - totalWithdrawnAllTime;
            if (pendingBalance <= 0 || !allPayments.Any()) return Array.Empty<byte>();

            // Filter payments to only include those after the last withdrawal period for each institute
            var pendingPayments = allPayments;

            string scopeText = instituteId.HasValue
                ? (pendingPayments.First().Class?.Institute?.InstituteName ?? "Specific Institute")
                : "All Institutes";

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // â”€â”€ Header (matches PLATFORM INVOICE style) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("PENDING EARNINGS REPORT").FontSize(20).Bold();
                            col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    // â”€â”€ Content â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        // Issued To block
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Issued To:").Bold();
                                c.Item().Text($"{tutor.FirstName} {tutor.LastName}");
                                if (!string.IsNullOrWhiteSpace(tutor.User?.Email))
                                    c.Item().Text(tutor.User.Email);
                                c.Item().Text("Role: Tutor");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Scope/Filter:").Bold();
                                c.Item().Text(scopeText);
                            });
                        });

                        // Group by institute, then class-wise summary
                        var byInstitute = pendingPayments
                            .GroupBy(p => p.Class?.Institute?.InstituteName ?? "Unknown Institute")
                            .OrderBy(g => g.Key)
                            .ToList();

                        decimal grandGross = 0, grandCommission = 0, grandPending = 0;

                        foreach (var instGroup in byInstitute)
                        {
                            col.Item().PaddingTop(20).Text($"Institute: {instGroup.Key}").Bold();

                            ClassTotals instTotals = default!;
                            col.Item().PaddingTop(5).Element(e => { instTotals = RenderClassSummaryRows(e, instGroup); });
                            decimal ig = instTotals?.Gross ?? 0;
                            decimal ic = instTotals?.Commission ?? 0;
                            decimal ip = instTotals?.Pending ?? 0;
                            grandGross += ig; grandCommission += ic; grandPending += ip;
                        }

                        // Grand totals table
                        col.Item().PaddingTop(20).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(130); });

                            t.Header(h =>
                            {
                                h.Cell().ColumnSpan(2).PaddingVertical(3).Text("EARNINGS SUMMARY").Bold();
                                h.Cell().ColumnSpan(2).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            void GRow(string lbl, string val, bool bold = false)
                            {
                                var l = t.Cell().PaddingVertical(2).Text(lbl); if (bold) l.Bold();
                                var r = t.Cell().AlignRight().PaddingVertical(2).Text(val); if (bold) r.Bold();
                            }

                            GRow("Total Gross Collected (All Classes)", $"{grandGross:N2}");
                            GRow("Total Institute Commission Deducted", $"- {grandCommission:N2}");
                            GRow("Total Net Accrued (All Time)", $"{grandPending:N2}");

                            t.Footer(f =>
                            {
                                f.Cell().ColumnSpan(2).PaddingVertical(3).BorderTop(1).BorderColor(Colors.Black);
                                f.Cell().PaddingVertical(2).Text("AVAILABLE EARNINGS (Take-Home)").Bold().FontSize(12);
                                f.Cell().AlignRight().PaddingVertical(2).Text($"{pendingBalance:N2}").Bold().FontSize(12);
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text("Note: This report shows currently un-withdrawn collected class fees.").Italic().FontSize(8);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // â”€â”€â”€ 3. Institute overview PDF â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<byte[]> GenerateInstitutePendingEarningsPdfAsync(Guid instituteId, Guid? tutorId)
        {
            // If the institute is downloading the report for a specific tutor, 
            // generate the exact same tutor-facing report so both sides see 100% identical PDFs.
            if (tutorId.HasValue && tutorId.Value != Guid.Empty)
            {
                return await GeneratePendingEarningsPdfAsync(tutorId.Value, instituteId);
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var institute = await _context.Institutes
                .FirstOrDefaultAsync(i => i.InstituteId == instituteId);
            
            if (institute == null) return Array.Empty<byte>();

            // Aggregate payments
            var classQuery = _context.Classes
                .Where(c => c.InstituteId == instituteId && !c.IsDeleted);
            if (tutorId.HasValue && tutorId.Value != Guid.Empty)
                classQuery = classQuery.Where(c => c.TutorId == tutorId.Value);

            var classIds = await classQuery.Select(c => c.ClassId).ToListAsync();

            // â”€â”€ Amount-based balance (matches the UI â€” totalTuition minus totalWithdrawn) â”€â”€
            var allPayments = await _context.ClassPayments
                .Include(p => p.Class).ThenInclude(c => c.Tutor)
                .Where(p => classIds.Contains(p.ClassId) && p.InstituteId == instituteId
                         && (p.Status == "Paid" || p.Status == "PAID"))
                .ToListAsync();

            decimal totalTuitionAllTime = allPayments.Sum(p => p.TuitionAmount ?? 0);

            decimal totalWithdrawnAllTime = await _context.Withdrawals
                .Where(w => w.InstituteId == instituteId
                         && (!tutorId.HasValue || w.TutorId == tutorId.Value))
                .SumAsync(w => w.WithdrawalAmount);

            decimal availableBalance = totalTuitionAllTime - totalWithdrawnAllTime;
            if (availableBalance <= 0 || !allPayments.Any()) return Array.Empty<byte>();

            // Filter payments to only include those after the last withdrawal period for each tutor
            var pendingPayments = allPayments;

            string scopeText = tutorId.HasValue
                ? ((pendingPayments.First().Class?.Tutor?.FirstName ?? "") + " " + (pendingPayments.First().Class?.Tutor?.LastName ?? "")).Trim()
                : "All Tutors";

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // â”€â”€ Header (matches PLATFORM INVOICE style) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("PENDING PAYOUTS REPORT").FontSize(20).Bold();
                            col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    // â”€â”€ Content â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        // Issued To block
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Issued To:").Bold();
                                c.Item().Text(institute.InstituteName);
                                c.Item().Text("Role: Institute");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Scope/Filter:").Bold();
                                c.Item().Text(scopeText);
                            });
                        });

                        // Group by Tutor, then class-wise summary rows
                        var byTutor = pendingPayments
                            .GroupBy(p => new { TId = p.Class?.TutorId, Name = p.Class?.Tutor != null ? $"{p.Class.Tutor.FirstName} {p.Class.Tutor.LastName}".Trim() : "Unknown Tutor" })
                            .OrderBy(g => g.Key.Name)
                            .ToList();

                        decimal grandGross = 0, grandCommission = 0, grandPending = 0;

                        foreach (var tutGroup in byTutor)
                        {
                            col.Item().PaddingTop(20).Text($"Tutor: {tutGroup.Key.Name}").Bold();

                            ClassTotals tutTotals = default!;
                            col.Item().PaddingTop(5).Element(e => { tutTotals = RenderClassSummaryRows(e, tutGroup); });
                            decimal tg = tutTotals?.Gross ?? 0;
                            decimal tc = tutTotals?.Commission ?? 0;
                            decimal tp = tutTotals?.Pending ?? 0;
                                                        grandGross += tg; grandCommission += tc; grandPending += tp;
                        }

                        // Grand totals table
                        col.Item().PaddingTop(20).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(130); });

                            t.Header(h =>
                            {
                                h.Cell().ColumnSpan(2).PaddingVertical(3).Text("PAYOUTS SUMMARY").Bold();
                                h.Cell().ColumnSpan(2).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            void GRow(string lbl, string val, bool bold = false)
                            {
                                var l = t.Cell().PaddingVertical(2).Text(lbl); if (bold) l.Bold();
                                var r = t.Cell().AlignRight().PaddingVertical(2).Text(val); if (bold) r.Bold();
                            }

                            GRow("Total Gross Collected (All Tutors & Classes)", $"{grandGross:N2}");
                            GRow("Total Institute Commission Earned", $"{grandCommission:N2}");
                            GRow("Total Tutor Net Accrued (All Time)", $"{grandPending:N2}");

                            t.Footer(f =>
                            {
                                f.Cell().ColumnSpan(2).PaddingVertical(3).BorderTop(1).BorderColor(Colors.Black);
                                f.Cell().PaddingVertical(2).Text("AVAILABLE FOR PAYOUT (LKR)").Bold().FontSize(14);
                                f.Cell().AlignRight().PaddingVertical(2).Text($"{availableBalance:N2}").Bold().FontSize(14);
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text("Note: This report shows currently un-withdrawn collected class fees per tutor.").Italic().FontSize(8);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
