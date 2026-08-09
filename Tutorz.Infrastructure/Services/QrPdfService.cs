using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    public class QrPdfService : IQrPdfService
    {
        private readonly TutorzDbContext _context;

        public QrPdfService(TutorzDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateUserQrPdfAsync(Guid id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Student? targetStudent = null;

            var user = await _context.Users
                .Include(u => u.Tutor)
                .Include(u => u.Students)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                targetStudent = user.Students.FirstOrDefault();
            }
            else
            {
                targetStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == id);
                if (targetStudent != null)
                {
                    user = await _context.Users
                        .Include(u => u.Tutor)
                        .Include(u => u.Students)
                        .FirstOrDefaultAsync(u => u.UserId == targetStudent.UserId);
                }
                else
                {
                    var dbTutor = await _context.Tutors.FirstOrDefaultAsync(t => t.TutorId == id);
                    if (dbTutor != null)
                    {
                        user = await _context.Users
                            .Include(u => u.Tutor)
                            .Include(u => u.Students)
                            .FirstOrDefaultAsync(u => u.UserId == dbTutor.UserId);
                    }
                    else
                    {
                        var dbInstitute = await _context.Institutes.FirstOrDefaultAsync(i => i.InstituteId == id);
                        if (dbInstitute != null)
                        {
                            user = await _context.Users
                                .Include(u => u.Tutor)
                                .Include(u => u.Students)
                                .FirstOrDefaultAsync(u => u.UserId == dbInstitute.UserId);
                        }
                    }
                }
            }

            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var firstName = "";
            var lastName = "";
            var registrationNumber = user.RegistrationNumber;
            var userId = user.UserId;

            if (targetStudent != null)
            {
                firstName = targetStudent.FirstName;
                lastName = targetStudent.LastName;
                registrationNumber = !string.IsNullOrEmpty(targetStudent.RegistrationNumber) ? targetStudent.RegistrationNumber : user.RegistrationNumber;
            }
            else if (user.Tutor != null)
            {
                firstName = user.Tutor.FirstName;
                lastName = user.Tutor.LastName;
            }
            else
            {
                var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == userId);
                if (institute != null)
                {
                    firstName = institute.InstituteName; // Use institute name as first name
                }
                else
                {
                    // Fallback for an admin or unlinked user
                    firstName = "User";
                }
            }

            var phoneNumber = user.PhoneNumber ?? "";
            if (phoneNumber.StartsWith("+94"))
            {
                phoneNumber = "0" + phoneNumber.Substring(3);
            }

            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "SmallLogo.png");
            byte[]? logoBytes = null;
            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(logoPath);
            }

            var document = Document.Create(container =>
            {
                // Front side
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(10, Unit.Millimetre);
                    page.PageColor(Colors.White);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(2, Unit.Millimetre).Container()
                            .Height(53.98f, Unit.Millimetre) // Standard ID height
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(5, Unit.Millimetre)
                            .Row(row =>
                            {
                                // Left: QR Code
                                var qrData = !string.IsNullOrEmpty(registrationNumber) ? registrationNumber : user.UserId.ToString();
                                row.ConstantItem(42, Unit.Millimetre)
                                    .AlignMiddle()
                                    .Image(GenerateQrCode(qrData));

                                // Right: Info
                                row.RelativeItem().PaddingLeft(5, Unit.Millimetre).AlignMiddle().Column(info =>
                                {
                                    var fName = firstName ?? "";
                                    var lName = lastName ?? "";
                                    var fullName = string.IsNullOrWhiteSpace(lName) ? fName : $"{fName} {lName}";

                                    if (fullName.Length > 15 && !string.IsNullOrWhiteSpace(lName))
                                    {
                                        float fnSize = Math.Max(6f, fName.Length > 15 ? 12f * 15f / fName.Length : 12f);
                                        float lnSize = Math.Max(6f, lName.Length > 15 ? 12f * 15f / lName.Length : 12f);
                                        info.Item().Text(fName).FontSize(fnSize).Bold().FontColor(Colors.Black);
                                        info.Item().Text(lName).FontSize(lnSize).Bold().FontColor(Colors.Black);
                                    }
                                    else
                                    {
                                        info.Item().Text(fullName).FontSize(12).Bold().FontColor(Colors.Black);
                                    }
                                    info.Item().Text(registrationNumber ?? "")
                                        .FontSize(9).FontColor(Colors.Grey.Darken3);
                                    info.Item().Text(phoneNumber)
                                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                                    info.Item().PaddingTop(4).Text("tutorz.lk")
                                        .FontSize(8).FontColor(Colors.Blue.Medium);
                                });
                            });
                    });
                });

                // Back side
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(10, Unit.Millimetre);
                    page.PageColor(Colors.White);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell(); // Empty cell to shift to the right side
                        RenderBackCard(table.Cell(), logoBytes);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateClassQrCodesPdfAsync(Guid classId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var cls = await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new { c.ClassName, c.Subject, c.Grade })
                .FirstOrDefaultAsync();

            if (cls == null)
            {
                throw new Exception("Class not found.");
            }

            var students = await _context.Enrollments
                .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Approved)
                .Select(e => new
                {
                    e.Student.UserId,
                    RegistrationNumber = !string.IsNullOrEmpty(e.Student.RegistrationNumber) ? e.Student.RegistrationNumber : e.Student.User.RegistrationNumber,
                    e.Student.FirstName,
                    e.Student.LastName,
                    PhoneNumber = e.Student.User.PhoneNumber
                })
                .ToListAsync();

            if (!students.Any())
            {
                throw new Exception("No approved students found in this class.");
            }

            // Cards per page setting
            const int cardsPerRow = 2;
            const int rowsPerPage = 4;
            const int cardsPerPage = cardsPerRow * rowsPerPage;

            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "SmallLogo.png");
            byte[]? logoBytes = null;
            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(logoPath);
            }

            // Pre-generate all QR codes in parallel before building the PDF
            // This is the most CPU-intensive step — parallelising it cuts generation time significantly
            var qrCache = new ConcurrentDictionary<string, byte[]>();
            await Task.WhenAll(students.Select(student => Task.Run(() =>
            {
                var key = !string.IsNullOrEmpty(student.RegistrationNumber)
                    ? student.RegistrationNumber
                    : student.UserId.ToString();
                qrCache.TryAdd(key, GenerateQrCode(key));
            })));

            var document = Document.Create(container =>
            {
                var totalPages = (int)Math.Ceiling((double)students.Count / cardsPerPage);

                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    var pageStudents = students.Skip(pageIndex * cardsPerPage).Take(cardsPerPage).ToList();

                    // Front side
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(10, Unit.Millimetre);
                        page.PageColor(Colors.White);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            foreach (var student in pageStudents)
                            {
                                var phoneNumber = student.PhoneNumber ?? "";
                                if (phoneNumber.StartsWith("+94"))
                                {
                                    phoneNumber = "0" + phoneNumber.Substring(3);
                                }

                                table.Cell().Padding(2, Unit.Millimetre).Container()
                                    .Height(53.98f, Unit.Millimetre) // Standard ID height
                                    .Border(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5, Unit.Millimetre)
                                    .Row(row =>
                                    {
                                        // Left: QR Code (use pre-generated cache)
                                        var qrData = !string.IsNullOrEmpty(student.RegistrationNumber) ? student.RegistrationNumber : student.UserId.ToString();
                                        row.ConstantItem(42, Unit.Millimetre)
                                            .AlignMiddle()
                                            .Image(qrCache.TryGetValue(qrData, out var cachedQr) ? cachedQr : GenerateQrCode(qrData));

                                        // Right: Info
                                        row.RelativeItem().PaddingLeft(5, Unit.Millimetre).AlignMiddle().Column(info =>
                                        {
                                            var fName = student.FirstName ?? "";
                                            var lName = student.LastName ?? "";
                                            var fullName = string.IsNullOrWhiteSpace(lName) ? fName : $"{fName} {lName}";

                                            if (fullName.Length > 15 && !string.IsNullOrWhiteSpace(lName))
                                            {
                                                float fnSize = Math.Max(6f, fName.Length > 15 ? 12f * 15f / fName.Length : 12f);
                                                float lnSize = Math.Max(6f, lName.Length > 15 ? 12f * 15f / lName.Length : 12f);
                                                info.Item().Text(fName).FontSize(fnSize).Bold().FontColor(Colors.Black);
                                                info.Item().Text(lName).FontSize(lnSize).Bold().FontColor(Colors.Black);
                                            }
                                            else
                                            {
                                                info.Item().Text(fullName).FontSize(12).Bold().FontColor(Colors.Black);
                                            }
                                            info.Item().Text(student.RegistrationNumber ?? "")
                                                .FontSize(9).FontColor(Colors.Grey.Darken3);
                                            info.Item().Text(phoneNumber)
                                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                                            info.Item().PaddingTop(4).Text("tutorz.lk")
                                                .FontSize(8).FontColor(Colors.Blue.Medium);
                                        });
                                    });
                            }
                        });
                    });

                    // Back side
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(10, Unit.Millimetre);
                        page.PageColor(Colors.White);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            // The back page is mirrored horizontally to align perfectly on duplex printouts
                            for (int i = 0; i < pageStudents.Count; i += 2)
                            {
                                if (i + 1 < pageStudents.Count)
                                {
                                    // Full row with 2 cards.
                                    RenderBackCard(table.Cell(), logoBytes);
                                    RenderBackCard(table.Cell(), logoBytes);
                                }
                                else
                                {
                                    // Row with 1 card. The front is on the Left, so the back MUST be on the Right.
                                    table.Cell(); // Empty cell on the Left
                                    RenderBackCard(table.Cell(), logoBytes); // Card on the Right
                                }
                            }
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private void RenderBackCard(QuestPDF.Infrastructure.IContainer container, byte[]? logoBytes)
        {
            container.Padding(2, Unit.Millimetre).Container()
                .Height(53.98f, Unit.Millimetre) // Standard ID height
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .AlignCenter()
                .AlignMiddle()
                .Column(col =>
                {
                    if (logoBytes != null)
                    {
                        col.Item().AlignCenter().Height(30, Unit.Millimetre).Image(logoBytes).FitArea();
                    }
                    else
                    {
                        col.Item().AlignCenter().Text("Tutorz").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    }
                    col.Item().AlignCenter().PaddingTop(4).Text("Tutorz Platform - www.tutorz.lk").FontSize(10);
                    col.Item().AlignCenter().Text("Empowering Education").FontSize(8).FontColor(Colors.Grey.Medium);
                });
        }

        private byte[] GenerateQrCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            // ECCLevel.M = ~15% error correction — smaller matrix than Q, still reliable for printing
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            // 5 pixels per module → ~150x150px PNG ≈ 3-5 KB instead of 50-100 KB at size 20
            return qrCode.GetGraphic(5);
        }
    }
}
