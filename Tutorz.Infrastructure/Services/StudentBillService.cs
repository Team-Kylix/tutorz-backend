using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// Generates student-facing class fee invoices as PDF bytes using QuestPDF.
    /// The invoice structure matches the platform's standard BillService layout
    /// (same header, "Billed To", period, line-item table, totals footer, status).
    /// </summary>
    public class StudentBillService : IStudentBillService
    {
        private readonly TutorzDbContext _context;
        private static readonly TimeZoneInfo SriLankaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");

        public StudentBillService(TutorzDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Generates a PDF invoice for a single ClassPayment record.
        /// Returns null if the payment is not found or the requesting student is not authorised.
        /// </summary>
        public async Task<byte[]?> GenerateClassPaymentPdfAsync(Guid paymentId, Guid studentId)
        {
            // ── Load payment with all needed relations ────────────────────────
            var payment = await _context.ClassPayments
                .Include(p => p.Class)
                    .ThenInclude(c => c.Tutor)
                .Include(p => p.Class)
                    .ThenInclude(c => c.Institute)
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.StudentId == studentId);

            if (payment == null) return null;

            var student   = payment.Student;
            var cls       = payment.Class;
            var tutor     = cls?.Tutor;
            var institute = cls?.Institute;

            string studentName = $"{student.FirstName} {student.LastName}".Trim();
            string tutorName   = tutor != null ? $"{tutor.FirstName} {tutor.LastName}".Trim() : "–";
            string className   = cls?.ClassName ?? cls?.Subject ?? "Class";
            string subject     = cls?.Subject    ?? className;
            string grade       = cls?.Grade      ?? "";
            string period      = new DateTime(payment.Year, payment.Month, 1).ToString("MMMM yyyy");
            string reference   = $"STU-{payment.Year % 100:D2}{payment.Month:D2}-{payment.PaymentId.ToString()[..6].ToUpper()}";
            bool   isPaid      = payment.Status == "Paid" || payment.Status == "PAID";

            decimal classFee   = cls?.Fee ?? payment.AmountPaid;
            decimal amountPaid = payment.AmountPaid;
            decimal gatewaySurcharge = isPaid ? Math.Max(0, amountPaid - classFee) : 0;

            // ── Assets ───────────────────────────────────────────────────────
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            // ── Build PDF document ────────────────────────────────────────────
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ── HEADER ────────────────────────────────────────────────
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);

                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("CLASS FEE INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Ref #: {reference}");
                            col.Item().Text($"Date:  {DateTime.UtcNow:dd MMM yyyy}");
                        });
                    });

                    // ── CONTENT ───────────────────────────────────────────────
                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        // Billed To / Period
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(studentName);
                                if (!string.IsNullOrWhiteSpace(student.RegistrationNumber))
                                    c.Item().Text(student.RegistrationNumber);
                                if (!string.IsNullOrWhiteSpace(student.Address))
                                    c.Item().Text(student.Address);
                                if (student.User?.Email != null)
                                    c.Item().Text(student.User.Email);
                                c.Item().Text("Role: Student");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Billing Period:").Bold();
                                c.Item().Text(period);
                                if (isPaid)
                                    c.Item().Text($"Paid: {payment.PaidAt:dd MMM yyyy}");
                            });
                        });

                        // Line-item table
                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);   // #
                                columns.RelativeColumn();     // Description
                                columns.ConstantColumn(100);  // Details
                                columns.ConstantColumn(80);   // Amount
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Details");
                                header.Cell().AlignRight().Text("Amount (LKR)");

                                header.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            // Row 1 – Class fee
                            string classDesc = string.IsNullOrWhiteSpace(grade)
                                ? $"{subject}"
                                : $"{subject} ({grade})";
                            if (institute != null)
                                classDesc += $" – {institute.InstituteName}";

                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text($"Class Fee – {classDesc}");
                            table.Cell().AlignRight().Text(period);
                            table.Cell().AlignRight().Text($"{classFee:N2}");

                            // Row 2 – Tutor (informational)
                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("Tutor");
                            table.Cell().AlignRight().Text(tutorName);
                            table.Cell().AlignRight().Text("–");

                            // Row 3 – Online gateway surcharge (only if > 0)
                            if (gatewaySurcharge > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Online Payment Gateway Surcharge");
                                table.Cell().AlignRight().Text("PayHere");
                                table.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                            }

                            // Footer totals
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderTop(1).BorderColor(Colors.Black);

                                footer.Cell().ColumnSpan(3).AlignRight().Text("Class Fee").Bold();
                                footer.Cell().AlignRight().Text($"{classFee:N2}");

                                if (gatewaySurcharge > 0)
                                {
                                    footer.Cell().ColumnSpan(3).AlignRight().Text("Gateway Surcharge").Bold();
                                    footer.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                                }

                                footer.Cell().ColumnSpan(3).AlignRight().PaddingTop(5)
                                    .Text("TOTAL PAID (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5)
                                    .Text($"{amountPaid:N2}").FontSize(14).Bold();
                            });
                        });

                        // Status + notes
                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span(isPaid ? "PAID" : "DUE").Bold()
                                    .FontColor(isPaid ? Colors.Green.Medium : Colors.Red.Medium);
                            });

                            if (!string.IsNullOrWhiteSpace(payment.Note))
                                c.Item().Text($"Note: {payment.Note}");

                            c.Item().PaddingTop(10)
                                .Text("Note: This is a system-generated class fee invoice.")
                                .Italic().FontSize(8);
                        });
                    });

                    // ── FOOTER ────────────────────────────────────────────────
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GenerateClassPaymentPdfForTutorAsync(Guid paymentId, Guid tutorId)
        {
            // Load payment — validate via the class's TutorId, not StudentId
            var payment = await _context.ClassPayments
                .Include(p => p.Class)
                    .ThenInclude(c => c.Tutor)
                .Include(p => p.Class)
                    .ThenInclude(c => c.Institute)
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId
                                       && p.Class != null
                                       && p.Class.TutorId == tutorId);

            if (payment == null) return null;

            var student   = payment.Student;
            var cls       = payment.Class;
            var tutor     = cls?.Tutor;
            var institute = cls?.Institute;

            string studentName = $"{student.FirstName} {student.LastName}".Trim();
            string tutorName   = tutor != null ? $"{tutor.FirstName} {tutor.LastName}".Trim() : "–";
            string className   = cls?.ClassName ?? cls?.Subject ?? "Class";
            string subject     = cls?.Subject    ?? className;
            string grade       = cls?.Grade      ?? "";
            string period      = new DateTime(payment.Year, payment.Month, 1).ToString("MMMM yyyy");
            string reference   = $"TUT-{payment.Year % 100:D2}{payment.Month:D2}-{payment.PaymentId.ToString()[..6].ToUpper()}";
            bool   isPaid      = payment.Status == "Paid" || payment.Status == "PAID";

            decimal classFee        = cls?.Fee ?? payment.AmountPaid;
            decimal amountPaid      = payment.AmountPaid;
            decimal gatewaySurcharge = isPaid ? Math.Max(0, amountPaid - classFee) : 0;

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);

                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("CLASS FEE INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Ref #: {reference}");
                            col.Item().Text($"Date:  {DateTime.UtcNow:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(studentName);
                                if (!string.IsNullOrWhiteSpace(student.RegistrationNumber))
                                    c.Item().Text(student.RegistrationNumber);
                                if (!string.IsNullOrWhiteSpace(student.Address))
                                    c.Item().Text(student.Address);
                                if (student.User?.Email != null)
                                    c.Item().Text(student.User.Email);
                                c.Item().Text("Role: Student");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Billing Period:").Bold();
                                c.Item().Text(period);
                                if (isPaid)
                                    c.Item().Text($"Paid: {payment.PaidAt:dd MMM yyyy}");
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Details");
                                header.Cell().AlignRight().Text("Amount (LKR)");

                                header.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            string classDesc = string.IsNullOrWhiteSpace(grade)
                                ? $"{subject}"
                                : $"{subject} ({grade})";
                            if (institute != null)
                                classDesc += $" – {institute.InstituteName}";

                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text($"Class Fee – {classDesc}");
                            table.Cell().AlignRight().Text(period);
                            table.Cell().AlignRight().Text($"{classFee:N2}");

                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("Tutor");
                            table.Cell().AlignRight().Text(tutorName);
                            table.Cell().AlignRight().Text("–");

                            if (gatewaySurcharge > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Online Payment Gateway Surcharge");
                                table.Cell().AlignRight().Text("PayHere");
                                table.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderTop(1).BorderColor(Colors.Black);

                                footer.Cell().ColumnSpan(3).AlignRight().Text("Class Fee").Bold();
                                footer.Cell().AlignRight().Text($"{classFee:N2}");

                                if (gatewaySurcharge > 0)
                                {
                                    footer.Cell().ColumnSpan(3).AlignRight().Text("Gateway Surcharge").Bold();
                                    footer.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                                }

                                footer.Cell().ColumnSpan(3).AlignRight().PaddingTop(5)
                                    .Text("TOTAL PAID (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5)
                                    .Text($"{amountPaid:N2}").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span(isPaid ? "PAID" : "DUE").Bold()
                                    .FontColor(isPaid ? Colors.Green.Medium : Colors.Red.Medium);
                            });

                            if (!string.IsNullOrWhiteSpace(payment.Note))
                                c.Item().Text($"Note: {payment.Note}");

                            c.Item().PaddingTop(10)
                                .Text("Note: This is a system-generated class fee invoice.")
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

        /// <inheritdoc/>
        public async Task<byte[]?> GenerateClassPaymentPdfForInstituteAsync(Guid paymentId, Guid instituteId)
        {
            // Load payment — validate via the InstituteId on the payment record
            var payment = await _context.ClassPayments
                .Include(p => p.Class)
                    .ThenInclude(c => c.Tutor)
                .Include(p => p.Class)
                    .ThenInclude(c => c.Institute)
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId
                                       && p.InstituteId == instituteId);

            if (payment == null) return null;

            var student   = payment.Student;
            var cls       = payment.Class;
            var tutor     = cls?.Tutor;
            var institute = cls?.Institute;

            string studentName = $"{student.FirstName} {student.LastName}".Trim();
            string tutorName   = tutor != null ? $"{tutor.FirstName} {tutor.LastName}".Trim() : "–";
            string className   = cls?.ClassName ?? cls?.Subject ?? "Class";
            string subject     = cls?.Subject    ?? className;
            string grade       = cls?.Grade      ?? "";
            string period      = new DateTime(payment.Year, payment.Month, 1).ToString("MMMM yyyy");
            string reference   = $"INS-{payment.Year % 100:D2}{payment.Month:D2}-{payment.PaymentId.ToString()[..6].ToUpper()}";
            bool   isPaid      = payment.Status == "Paid" || payment.Status == "PAID";

            decimal classFee         = cls?.Fee ?? payment.AmountPaid;
            decimal amountPaid       = payment.AmountPaid;
            decimal gatewaySurcharge = isPaid ? Math.Max(0, amountPaid - classFee) : 0;

            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(44).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);

                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("CLASS FEE INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Ref #: {reference}");
                            col.Item().Text($"Date:  {DateTime.UtcNow:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(studentName);
                                if (!string.IsNullOrWhiteSpace(student.RegistrationNumber))
                                    c.Item().Text(student.RegistrationNumber);
                                if (!string.IsNullOrWhiteSpace(student.Address))
                                    c.Item().Text(student.Address);
                                if (student.User?.Email != null)
                                    c.Item().Text(student.User.Email);
                                c.Item().Text("Role: Student");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Billing Period:").Bold();
                                c.Item().Text(period);
                                if (isPaid)
                                    c.Item().Text($"Paid: {payment.PaidAt:dd MMM yyyy}");
                                if (institute != null)
                                    c.Item().Text($"Institute: {institute.InstituteName}");
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Details");
                                header.Cell().AlignRight().Text("Amount (LKR)");

                                header.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            string classDesc = string.IsNullOrWhiteSpace(grade)
                                ? $"{subject}"
                                : $"{subject} ({grade})";
                            if (institute != null)
                                classDesc += $" – {institute.InstituteName}";

                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text($"Class Fee – {classDesc}");
                            table.Cell().AlignRight().Text(period);
                            table.Cell().AlignRight().Text($"{classFee:N2}");

                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("Tutor");
                            table.Cell().AlignRight().Text(tutorName);
                            table.Cell().AlignRight().Text("–");

                            if (gatewaySurcharge > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Online Payment Gateway Surcharge");
                                table.Cell().AlignRight().Text("PayHere");
                                table.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).PaddingVertical(5)
                                    .BorderTop(1).BorderColor(Colors.Black);

                                footer.Cell().ColumnSpan(3).AlignRight().Text("Class Fee").Bold();
                                footer.Cell().AlignRight().Text($"{classFee:N2}");

                                if (gatewaySurcharge > 0)
                                {
                                    footer.Cell().ColumnSpan(3).AlignRight().Text("Gateway Surcharge").Bold();
                                    footer.Cell().AlignRight().Text($"{gatewaySurcharge:N2}");
                                }

                                footer.Cell().ColumnSpan(3).AlignRight().PaddingTop(5)
                                    .Text("TOTAL PAID (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5)
                                    .Text($"{amountPaid:N2}").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span(isPaid ? "PAID" : "DUE").Bold()
                                    .FontColor(isPaid ? Colors.Green.Medium : Colors.Red.Medium);
                            });

                            if (!string.IsNullOrWhiteSpace(payment.Note))
                                c.Item().Text($"Note: {payment.Note}");

                            c.Item().PaddingTop(10)
                                .Text("Note: This is a system-generated class fee invoice.")
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
    }
}
