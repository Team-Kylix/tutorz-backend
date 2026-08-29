using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tutorz.Infrastructure.Services
{
    public class BillService : IBillService
    {
        private readonly TutorzDbContext _context;

        public BillService(TutorzDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<ServiceResponse<BillPagedResult>> GetAllBillsAsync(string? search, int page, int pageSize)
        {
            var query = _context.MonthlyBills.Include(b => b.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.BillNumber.Contains(search) || b.User.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillSummaryDto
                {
                    BillId = b.Id,
                    BillReference = b.BillNumber,
                    UserId = b.UserId,
                    Email = b.User.Email,
                    Month = b.Month,
                    Year = b.Year,
                    MonthYear = b.Year + "-" + b.Month.ToString("D2"),
                    PayableAmount = b.BillAmount,
                    PaidAmount = b.PaidAmount,
                    Status = b.IsPaid ? "Paid" : (b.DueAmount > 0 ? "Overdue" : "Unpaid"),
                    GeneratedAt = b.CreatedAt
                })
                .ToListAsync();

            return ServiceResponse<BillPagedResult>.SuccessResponse(new BillPagedResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResponse<BillDetailDto>> GetBillByIdAsync(Guid billId, Guid requestingUserId, string requestingRole)
        {
            var bill = await _context.MonthlyBills.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null) return ServiceResponse<BillDetailDto>.ErrorResponse("Bill not found.");

            if (requestingRole != "Admin" && requestingRole != "SuperAdmin" && bill.UserId != requestingUserId)
            {
                return ServiceResponse<BillDetailDto>.ErrorResponse("Unauthorized access to this bill.");
            }

            var dto = new BillDetailDto
            {
                BillId = bill.Id,
                BillReference = bill.BillNumber,
                UserId = bill.UserId,
                Email = bill.User.Email,
                Month = bill.Month,
                Year = bill.Year,
                MonthYear = bill.Year + "-" + bill.Month.ToString("D2"),
                GeneratedAt = bill.CreatedAt,
                PayableAmount = bill.BillAmount,
                PaidAmount = bill.PaidAmount,
                Status = bill.IsPaid ? "Paid" : (bill.DueAmount > 0 ? "Overdue" : "Unpaid")
            };

            return ServiceResponse<BillDetailDto>.SuccessResponse(dto);
        }

        public async Task<ServiceResponse<bool>> MarkBillAsPaidAsync(Guid billId)
        {
            var bill = await _context.MonthlyBills.FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null) return ServiceResponse<bool>.ErrorResponse("Bill not found.");

            bill.IsPaid = true;
            bill.PaidAmount = bill.BillAmount;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
 
        public async Task<ServiceResponse<BillPagedResult>> GetMyBillsAsync(Guid userId, int page, int pageSize)
        {
            var query = _context.MonthlyBills.Where(b => b.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillSummaryDto
                {
                    BillId = b.Id,
                    BillReference = b.BillNumber,
                    UserId = b.UserId,
                    Month = b.Month,
                    Year = b.Year,
                    MonthYear = b.Year + "-" + b.Month.ToString("D2"),
                    PayableAmount = b.BillAmount,
                    PaidAmount = b.PaidAmount,
                    Status = b.IsPaid ? "Paid" : (b.DueAmount > 0 ? "Overdue" : "Unpaid"),
                    GeneratedAt = b.CreatedAt
                })
                .ToListAsync();

            return ServiceResponse<BillPagedResult>.SuccessResponse(new BillPagedResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResponse<byte[]>> GenerateBillPdfAsync(Guid billId, Guid requestingUserId)
        {
            var bill = await _context.MonthlyBills.FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null) return ServiceResponse<byte[]>.ErrorResponse("Bill not found.");

            if (bill.UserId != requestingUserId) return ServiceResponse<byte[]>.ErrorResponse("Unauthorized access to this bill.");

            // --- DYNAMIC RECALCULATION ---
            var existingUsage = await _context.MonthlyUsageSummaries.FirstOrDefaultAsync(u => u.UserId == bill.UserId && u.Month == bill.Month && u.Year == bill.Year);
            if (existingUsage == null)
            {
                existingUsage = new Tutorz.Domain.Entities.MonthlyUsageSummary
                {
                    Id = Guid.NewGuid(),
                    UserId = bill.UserId,
                    UserRole = bill.IsIndividual ? "Tutor" : "Institute",
                    Month = bill.Month,
                    Year = bill.Year,
                    LastUpdated = DateTime.UtcNow
                };
                _context.MonthlyUsageSummaries.Add(existingUsage);
            }

            decimal storageServerCost = 0m;
            decimal billedServerCost = 0m;
            decimal billedCommissions = 0m;
            decimal billedTutorSmsCost = 0m; // Institute billed for Tutor SMS
            decimal currentSmsCost = 0m; // Statistical SMS cost for MonthlyUsageSummary
            decimal billedOwnSmsCost = 0m; // Sms cost billed directly to this user

            if (bill.IsIndividual)
            {
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == bill.UserId);
                if (tutor != null)
                {
                    var indivAtts = await _context.Attendances.Include(a => a.Class)
                        .CountAsync(a => a.Class.TutorId == tutor.TutorId && a.Class.InstituteId == null && a.Date.Month == bill.Month && a.Date.Year == bill.Year);
                    var instAtts = await _context.Attendances.Include(a => a.Class)
                        .CountAsync(a => a.Class.TutorId == tutor.TutorId && a.Class.InstituteId != null && a.Date.Month == bill.Month && a.Date.Year == bill.Year);
                    
                    storageServerCost = (indivAtts + instAtts) * 1.5m;
                    billedServerCost = indivAtts * 1.5m;
                    
                    billedCommissions = await _context.MonthlyPlatformCommissionSummaries
                        .Where(c => c.Month == bill.Month && c.Year == bill.Year && c.InstituteId == null && c.TutorId == tutor.TutorId)
                        .SumAsync(c => c.PlatformTutorCommission);
                        
                    currentSmsCost = await _context.SmsLogs
                        .Where(l => l.BillTo == bill.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year)
                        .SumAsync(l => l.Cost); // Total SMS for this tutor (Individual + Institute)
                        
                    billedOwnSmsCost = await _context.SmsLogs
                        .Where(l => l.SenderUserId == bill.UserId && l.BillTo == bill.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year)
                        .SumAsync(l => l.Cost); // Billed directly to Tutor
                }
            }
            else
            {
                var inst = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == bill.UserId);
                if (inst != null)
                {
                    var instAtts = await _context.Attendances.Include(a => a.Class)
                        .CountAsync(a => a.Class.InstituteId == inst.InstituteId && a.Date.Month == bill.Month && a.Date.Year == bill.Year);
                    
                    storageServerCost = instAtts * 0.5m;
                    billedServerCost = instAtts * 2.0m;
                    
                    billedCommissions = await _context.MonthlyPlatformCommissionSummaries
                        .Where(c => c.Month == bill.Month && c.Year == bill.Year && c.InstituteId == inst.InstituteId)
                        .SumAsync(c => c.PlatformAllCommission);

                    currentSmsCost = await _context.SmsLogs
                        .Where(l => l.SenderUserId == inst.UserId && l.BillTo == inst.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year)
                        .SumAsync(l => l.Cost);
                        
                    billedOwnSmsCost = currentSmsCost;

                    billedTutorSmsCost = await _context.SmsLogs
                        .Where(l => l.SenderUserId == inst.UserId && l.BillTo != inst.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year)
                        .SumAsync(l => l.Cost);

                    // --- UPDATE RELATED TUTORS ---
                    var tutorIds = await _context.Classes
                        .Where(c => c.InstituteId == inst.InstituteId && c.TutorId != null)
                        .Select(c => c.TutorId)
                        .Distinct()
                        .ToListAsync();
                    
                    foreach (var tId in tutorIds)
                    {
                        var t = await _context.Tutors.FindAsync(tId);
                        if (t != null)
                        {
                            var tUsage = await _context.MonthlyUsageSummaries.FirstOrDefaultAsync(u => u.UserId == t.UserId && u.Month == bill.Month && u.Year == bill.Year);
                            var tBill = await _context.MonthlyBills.FirstOrDefaultAsync(b => b.UserId == t.UserId && b.Month == bill.Month && b.Year == bill.Year);
                            if (tUsage != null && tBill != null)
                            {
                                var iAtts = await _context.Attendances.Include(a => a.Class).CountAsync(a => a.Class.TutorId == t.TutorId && a.Class.InstituteId == null && a.Date.Month == bill.Month && a.Date.Year == bill.Year);
                                var inAtts = await _context.Attendances.Include(a => a.Class).CountAsync(a => a.Class.TutorId == t.TutorId && a.Class.InstituteId != null && a.Date.Month == bill.Month && a.Date.Year == bill.Year);
                                
                                // Total SMS for Tutor (Statistical)
                                var tSmsCost = await _context.SmsLogs.Where(l => l.BillTo == t.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year).SumAsync(l => l.Cost);
                                tUsage.SmsCost = tSmsCost;
                                tUsage.ServerCost = (iAtts + inAtts) * 1.5m;
                                tUsage.TotalCost = tUsage.SmsCost + tUsage.ServerCost;
                                tUsage.LastUpdated = DateTime.UtcNow;

                                var tComms = await _context.MonthlyPlatformCommissionSummaries
                                    .Where(c => c.Month == bill.Month && c.Year == bill.Year && c.InstituteId == null && c.TutorId == t.TutorId)
                                    .SumAsync(c => c.PlatformTutorCommission);
                                    
                                // Billed SMS for Tutor
                                var tBilledSmsCost = await _context.SmsLogs.Where(l => l.SenderUserId == t.UserId && l.BillTo == t.UserId && l.SentAt.Month == bill.Month && l.SentAt.Year == bill.Year).SumAsync(l => l.Cost);
                                
                                tBill.BillAmount = tComms + tBilledSmsCost + (iAtts * 1.5m);
                            }
                        }
                    }
                    // -----------------------------
                }
            }

            existingUsage.SmsCost = currentSmsCost;
            existingUsage.ServerCost = storageServerCost;
            existingUsage.TotalCost = existingUsage.SmsCost + existingUsage.ServerCost;
            existingUsage.LastUpdated = DateTime.UtcNow;
            
            bill.BillAmount = billedCommissions + billedOwnSmsCost + billedServerCost + billedTutorSmsCost;
            await _context.SaveChangesAsync();
            // -----------------------------

            if (bill.IsIndividual)
            {
                var tutorUserId = bill.UserId;
                var tMonth = bill.Month;
                var tYear = bill.Year;

                var tUser = await _context.Users.FindAsync(tutorUserId);
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == tutorUserId);
                if (tutor == null) return ServiceResponse<byte[]>.ErrorResponse("Tutor not found.");

                var tUsageSummary = await _context.MonthlyUsageSummaries
                    .FirstOrDefaultAsync(u => u.UserId == tutorUserId && u.Month == tMonth && u.Year == tYear);

                var tCommissions = await _context.MonthlyPlatformCommissionSummaries
                    .Include(c => c.Class)
                    .Where(c => c.TutorId == tutor.TutorId && c.InstituteId == null && c.Month == tMonth && c.Year == tYear)
                    .ToListAsync();

                var tDto = new InstituteBillPdfDto
                {
                    BillNumber = bill.BillNumber,
                    Role = "Tutor",
                    InstituteName = tutor.FirstName + " " + tutor.LastName,
                    Email = tUser?.Email ?? "",
                    Address = "",
                    RegistrationNumber = tUser?.RegistrationNumber,
                    Month = tMonth,
                    Year = tYear,
                    GeneratedAt = bill.CreatedAt,
                    IsPaid = bill.IsPaid,
                    SmsTotalCost = billedOwnSmsCost, // Only show individual SMS on Tutor's PDF
                    ServerTotalCost = billedServerCost,
                    PreviousOverdueAmount = bill.DueAmount,
                    TotalPayable = bill.BillAmount
                };
                tDto.SubTotal = tDto.TotalPayable - tDto.PreviousOverdueAmount;

                var classList = new System.Collections.Generic.List<ClassCommissionDto>();
                foreach(var c in tCommissions) 
                {
                    if (c.PlatformTutorCommission > 0)
                    {
                        classList.Add(new ClassCommissionDto
                        {
                            ClassName = c.Class.Grade + " " + c.Class.Subject,
                            Charge = c.PlatformTutorCommission
                        });
                    }
                }

                tDto.TutorCommissions = new System.Collections.Generic.List<TutorCommissionGroupDto>
                {
                    new TutorCommissionGroupDto
                    {
                        TutorName = "Individual Classes",
                        Classes = classList
                    }
                };

                var tPdfBytes = GeneratePdf(tDto);
                return ServiceResponse<byte[]>.SuccessResponse(tPdfBytes);
            }

            // Institute logic
            var instituteUserId = bill.UserId;
            var iMonth = bill.Month;
            var iYear = bill.Year;

            var iUser = await _context.Users.FindAsync(instituteUserId);
            var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == instituteUserId);
            if (institute == null) return ServiceResponse<byte[]>.ErrorResponse("Institute not found.");

            var iUsageSummary = await _context.MonthlyUsageSummaries
                .FirstOrDefaultAsync(u => u.UserId == instituteUserId && u.Month == iMonth && u.Year == iYear);

            var iCommissions = await _context.MonthlyPlatformCommissionSummaries
                .Include(c => c.Class)
                .Include(c => c.Tutor)
                .Where(c => c.InstituteId == institute.InstituteId && c.Month == iMonth && c.Year == iYear)
                .ToListAsync();

            var iDto = new InstituteBillPdfDto
            {
                BillNumber = bill.BillNumber,
                Role = "Institute",
                InstituteName = institute.InstituteName,
                Email = iUser?.Email ?? "",
                Address = institute.Address,
                RegistrationNumber = iUser?.RegistrationNumber,
                Month = iMonth,
                Year = iYear,
                GeneratedAt = bill.CreatedAt,
                IsPaid = bill.IsPaid,
                SmsTotalCost = iUsageSummary?.SmsCost ?? 0m,
                ServerTotalCost = storageServerCost,
                PreviousOverdueAmount = bill.DueAmount,
                TotalPayable = bill.BillAmount
            };

            iDto.SubTotal = iDto.TotalPayable - iDto.PreviousOverdueAmount;

            // Group by Tutor and inject the Tutor's Server Cost Share
            var groupedCommissionsList = new System.Collections.Generic.List<TutorCommissionGroupDto>();
            var groupedByTutor = iCommissions.GroupBy(c => c.Tutor);
            
            foreach (var g in groupedByTutor)
            {
                var classesList = new System.Collections.Generic.List<ClassCommissionDto>();
                foreach(var c in g) 
                {
                    var className = c.Class.Grade + " " + c.Class.Subject;
                    if (c.PlatformInstituteCommission > 0)
                        classesList.Add(new ClassCommissionDto { ClassName = className + " (Institute Share)", Charge = c.PlatformInstituteCommission });
                    if (c.PlatformTutorCommission > 0)
                        classesList.Add(new ClassCommissionDto { ClassName = className + " (Tutor Share)", Charge = c.PlatformTutorCommission });
                }

                // Add Tutor's Server Usage Share (1.5 per attendance)
                var tutorAtts = _context.Attendances.Include(a => a.Class)
                    .Count(a => a.Class.TutorId == g.Key.TutorId && a.Class.InstituteId == institute.InstituteId && a.Date.Month == iMonth && a.Date.Year == iYear);
                
                if (tutorAtts > 0)
                {
                    classesList.Add(new ClassCommissionDto { ClassName = "Server Usage (Tutor Share)", Charge = tutorAtts * 1.5m });
                }

                var tutorSmsCost = await _context.SmsLogs
                    .Where(l => l.SenderUserId == institute.UserId && l.BillTo == g.Key.UserId && l.SentAt.Month == iMonth && l.SentAt.Year == iYear)
                    .SumAsync(l => l.Cost);
                
                if (tutorSmsCost > 0)
                {
                    classesList.Add(new ClassCommissionDto { ClassName = "SMS Usage (Tutor Share)", Charge = tutorSmsCost });
                }

                if (classesList.Any())
                {
                    groupedCommissionsList.Add(new TutorCommissionGroupDto
                    {
                        TutorName = g.Key.FirstName + " " + g.Key.LastName,
                        Classes = classesList
                    });
                }
            }
            var groupedCommissions = groupedCommissionsList;

            iDto.TutorCommissions = groupedCommissions;

            var iPdfBytes = GeneratePdf(iDto);
            return ServiceResponse<byte[]>.SuccessResponse(iPdfBytes);
        }


        private byte[] GeneratePdf(InstituteBillPdfDto data)
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SmallLogo.png");
            bool hasLogo = File.Exists(logoPath);
            bool isPaid = data.IsPaid;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Rotate(-45)
                        .Text(text => 
                        {
                            text.AlignCenter();
                            text.Span(isPaid ? "PAID" : "UNPAID")
                                .FontSize(120)
                                .FontColor(isPaid ? "#334CAF50" : "#33F44336")
                                .Bold();
                        });

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (hasLogo)
                                col.Item().MaxHeight(70).Image(logoPath);
                            else
                                col.Item().Text("Tutorz.lk")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);

                            col.Item().Text("Kylix Technology");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("PLATFORM INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Bill #: {data.BillNumber}");
                            col.Item().Text($"Date: {data.GeneratedAt:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(data.InstituteName);
                                if (!string.IsNullOrWhiteSpace(data.RegistrationNumber))
                                    c.Item().Text(data.RegistrationNumber);
                                if (!string.IsNullOrWhiteSpace(data.Address))
                                    c.Item().Text(data.Address);
                                c.Item().Text(data.Email);
                                c.Item().Text($"Role: Institute");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Billing Period:").Bold();
                                c.Item().Text($"{new DateTime(data.Year, data.Month, 1):MMM yyyy}");
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);   // #
                                columns.RelativeColumn();     // Description
                                columns.ConstantColumn(80);   // Charge
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Charge");

                                header.Cell().ColumnSpan(3).PaddingVertical(5)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            foreach (var tutorGroup in data.TutorCommissions)
                            {
                                table.Cell().ColumnSpan(3).PaddingTop(5).PaddingBottom(2).Text(tutorGroup.TutorName).Bold();

                                decimal tutorShareTotal = 0;
                                decimal instShareTotal = 0;

                                foreach (var item in tutorGroup.Classes)
                                {
                                    table.Cell().Text($"{rowNum++}");
                                    table.Cell().PaddingLeft(10).Text($"{item.ClassName}");
                                    table.Cell().AlignRight().Text($"{item.Charge:N2}");

                                    if (item.ClassName.Contains("(Tutor Share)"))
                                        tutorShareTotal += item.Charge;
                                    else if (item.ClassName.Contains("(Institute Share)"))
                                        instShareTotal += item.Charge;
                                }

                                if (tutorShareTotal > 0 || instShareTotal > 0)
                                {
                                    table.Cell().ColumnSpan(2).AlignRight().PaddingTop(2).Text("Tutor Share Total:").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                                    table.Cell().AlignRight().PaddingTop(2).Text($"{tutorShareTotal:N2}").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);

                                    table.Cell().ColumnSpan(2).AlignRight().PaddingBottom(5).Text("Institute Share Total:").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                                    table.Cell().AlignRight().PaddingBottom(5).Text($"{instShareTotal:N2}").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                                }
                            }

                            bool hasPlatformServices = data.ServerTotalCost > 0 || data.SmsTotalCost > 0 || data.PreviousOverdueAmount > 0;
                            if (hasPlatformServices && data.TutorCommissions.Any())
                            {
                                table.Cell().ColumnSpan(3).PaddingTop(10).PaddingBottom(2).Text("Platform Services").Bold();
                            }

                            if (data.ServerTotalCost > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                string label = data.Role == "Institute" ? "Server Usage (Institute Share)" : "Server Usage (Attendance)";
                                table.Cell().PaddingLeft(10).Text(label);
                                table.Cell().AlignRight().Text($"{data.ServerTotalCost:N2}");
                            }

                            if (data.SmsTotalCost > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().PaddingLeft(10).Text("SMS Dispatch Service");
                                table.Cell().AlignRight().Text($"{data.SmsTotalCost:N2}");
                            }

                            if (data.PreviousOverdueAmount > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().PaddingLeft(10).Text("Previous Overdue Balance");
                                table.Cell().AlignRight().Text($"{data.PreviousOverdueAmount:N2}");
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).PaddingVertical(5)
                                    .BorderTop(1).BorderColor(Colors.Black);

                                footer.Cell().ColumnSpan(2).AlignRight().Text("Sub Total").Bold();
                                footer.Cell().AlignRight().Text($"{data.SubTotal:N2}");

                                footer.Cell().ColumnSpan(2).AlignRight().PaddingTop(5)
                                    .Text("TOTAL PAYABLE (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5)
                                    .Text($"{data.TotalPayable:N2}").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span(data.IsPaid ? "PAID" : "UNPAID").Bold()
                                    .FontColor(isPaid ? Colors.Green.Medium : Colors.Red.Medium);
                            });
                            c.Item().Text("Payment Terms: Please settle this invoice within 30 days.");
                            
                            c.Item().PaddingTop(10)
                                .Text("Note: This is a system-generated invoice for platform usage fees.")
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




