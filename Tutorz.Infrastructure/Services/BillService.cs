using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.DTOs;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace Tutorz.Infrastructure.Services
{
    public class BillService : IBillService
    {
        private readonly TutorzDbContext _context;
        private static readonly TimeZoneInfo SriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");

        public BillService(TutorzDbContext context)
        {
            _context = context;
            // QuestPDF license is required for version 2022.12.0 and later
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<ServiceResponse<BillPagedResult>> GetAllBillsAsync(string? search, int page, int pageSize)
        {
            var query = _context.Bills
                .Include(b => b.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.User.Email.Contains(search) || b.BillReference.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillSummaryDto
                {
                    BillId = b.BillId,
                    BillReference = b.BillReference,
                    UserId = b.UserId,
                    UserName = b.UserRole == "Student" 
                        ? _context.Students.Where(s => s.UserId == b.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault() ?? b.User.Email
                        : b.UserRole == "Tutor" 
                            ? _context.Tutors.Where(t => t.UserId == b.UserId).Select(t => t.FirstName + " " + t.LastName).FirstOrDefault() ?? b.User.Email
                            : b.UserRole == "Institute" 
                                ? _context.Institutes.Where(i => i.UserId == b.UserId).Select(i => i.InstituteName).FirstOrDefault() ?? b.User.Email
                                : b.User.Email,
                    Email = b.User.Email,
                    RegistrationNumber = b.User.RegistrationNumber,
                    MobileNumber = b.User.PhoneNumber,
                    UserRole = b.UserRole,
                    Month = b.Month,
                    Year = b.Year,
                    MonthYear = b.MonthYear,
                    PayableAmount = b.PayableAmount,
                    Status = b.Status,
                    GeneratedAt = b.GeneratedAt
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

        public async Task<ServiceResponse<BillPagedResult>> GetMyBillsAsync(Guid userId, int page, int pageSize)
        {
            var query = _context.Bills
                .Where(b => b.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillSummaryDto
                {
                    BillId = b.BillId,
                    BillReference = b.BillReference,
                    UserId = b.UserId,
                    Month = b.Month,
                    Year = b.Year,
                    MonthYear = b.MonthYear,
                    PayableAmount = b.PayableAmount,
                    Status = b.Status,
                    GeneratedAt = b.GeneratedAt
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
            var bill = await _context.Bills
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (bill == null) return ServiceResponse<BillDetailDto>.ErrorResponse("Bill not found.");

            // Security: Users can only see their own bills
            if (requestingRole != "Admin" && requestingRole != "SuperAdmin" && bill.UserId != requestingUserId)
            {
                return ServiceResponse<BillDetailDto>.ErrorResponse("Unauthorized access to this bill.");
            }

            var lastPaidBill = await _context.Bills
                .Where(b => b.UserId == bill.UserId && b.Status == BillStatus.Paid.ToString() && b.GeneratedAt < bill.GeneratedAt)
                .OrderByDescending(b => b.PaidAt)
                .FirstOrDefaultAsync();

            DateTime dynamicStartDate = lastPaidBill?.PaidAt != null
                ? TimeZoneInfo.ConvertTimeFromUtc(lastPaidBill.PaidAt.Value, SriLankaTimeZone)
                : bill.BillStartDate;

            DateTime dynamicEndDate = bill.Status == BillStatus.Paid.ToString() && bill.PaidAt.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(bill.PaidAt.Value, SriLankaTimeZone)
                : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SriLankaTimeZone);

            var dto = new BillDetailDto
            {
                BillId = bill.BillId,
                BillReference = bill.BillReference,
                UserId = bill.UserId,
                UserName = bill.UserRole == "Student" 
                    ? await _context.Students.Where(s => s.UserId == bill.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefaultAsync() ?? bill.User.Email
                    : bill.UserRole == "Tutor" 
                        ? await _context.Tutors.Where(t => t.UserId == bill.UserId).Select(t => t.FirstName + " " + t.LastName).FirstOrDefaultAsync() ?? bill.User.Email
                        : bill.UserRole == "Institute" 
                            ? await _context.Institutes.Where(i => i.UserId == bill.UserId).Select(i => i.InstituteName).FirstOrDefaultAsync() ?? bill.User.Email
                            : bill.User.Email,
                Email = bill.User.Email,
                RegistrationNumber = bill.User.RegistrationNumber,
                MobileNumber = bill.User.PhoneNumber,
                UserRole = bill.UserRole,
                Month = bill.Month,
                Year = bill.Year,
                MonthYear = bill.MonthYear,
                BillStartDate = dynamicStartDate,
                BillEndDate = dynamicEndDate,
                GeneratedAt = bill.GeneratedAt,
                ApiCallCount = bill.ApiCallCount,
                ApiCallRate = bill.ApiCallRate,
                ApiUsageAmount = bill.ApiUsageAmount,
                SmsSentCount = bill.SmsSentCount,
                SmsRate = bill.SmsRate,
                SmsAmount = bill.SmsAmount,
                PlatformCommissionAmount = bill.PlatformCommissionAmount,
                PreviousOverdueAmount = bill.PreviousOverdueAmount,
                SubTotal = bill.SubTotal,
                TaxPercentage = bill.TaxPercentage,
                TaxAmount = bill.TaxAmount,
                PayableAmount = bill.PayableAmount,
                Status = bill.Status,
                PaidAt = bill.PaidAt
            };

            // --- Aggregate Class Commissions Breakdown ---
            var config = (await GetBillingConfigAsync()).Data;
            dto.PlatformCommissionRate = config?.PlatformCommissionRate ?? 1.00m;

            // Fetch all payments for this user's role in the specified month/year
            IQueryable<ClassPayment> paymentQuery = _context.ClassPayments
                .Include(p => p.Class)
                .ThenInclude(c => c.Institute)
                .Where(p => p.Month == bill.Month && p.Year == bill.Year);

            if (bill.UserRole == "Tutor")
            {
                paymentQuery = paymentQuery.Where(p => p.Class.TutorId == bill.UserId);
            }
            else if (bill.UserRole == "Institute")
            {
                paymentQuery = paymentQuery.Where(p => p.InstituteId == bill.UserId);
            }
            else
            {
                // Students don't have platform commission breakdowns
                paymentQuery = paymentQuery.Where(p => false);
            }

            var payments = await paymentQuery.ToListAsync();

            // Group by Class to generate line items
            dto.ClassCommissions = payments
                .GroupBy(p => p.ClassId)
                .Select(g => {
                    var first = g.First();
                    var cls = first.Class;
                    
                    // Format: Grade + Subject + Institute
                    string className = $"{cls.Grade} {cls.Subject} {cls.Institute?.InstituteName}".Trim();
                    if (string.IsNullOrEmpty(className)) className = cls.ClassName ?? "Unknown Class";

                    decimal earnings = bill.UserRole == "Tutor" 
                        ? g.Sum(p => p.TuitionAmount ?? 0) 
                        : g.Sum(p => p.InstituteAmount ?? 0);

                    decimal commission = bill.UserRole == "Tutor"
                        ? g.Sum(p => p.TutorCommission ?? 0)
                        : g.Sum(p => p.InstituteCommission ?? 0);

                    return new ClassCommissionItemDto
                    {
                        ClassName = className,
                        Earnings = Math.Round(earnings, 2),
                        Rate = dto.PlatformCommissionRate,
                        Amount = Math.Round(commission, 2)
                    };
                })
                .Where(i => i.Earnings > 0)
                .OrderBy(i => i.ClassName)
                .ToList();

            return ServiceResponse<BillDetailDto>.SuccessResponse(dto);
        }

        public async Task<ServiceResponse<bool>> MarkBillAsPaidAsync(Guid billId)
        {
            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null) return ServiceResponse<bool>.ErrorResponse("Bill not found.");

            bill.Status = BillStatus.Paid.ToString();
            bill.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }

        public async Task<ServiceResponse<int>> FixOldBillReferencesAsync()
        {
            var bills = await _context.Bills.ToListAsync();
            int updatedCount = 0;
            
            foreach (var b in bills)
            {
                var parts = b.BillReference.Split('-');
                // Example: TZ-2026-05-EECB-93CB -> TZ-2026-05-EECB
                // Expected parts length with random Guid suffix is 5 (TZ, YYYY, MM, UID, GUID)
                if (parts.Length > 4) 
                {
                    var newRef = string.Join("-", parts.Take(4));
                    b.BillReference = newRef;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return ServiceResponse<int>.SuccessResponse(updatedCount, $"Successfully updated {updatedCount} bill references.");
        }

        public async Task<ServiceResponse<BillingConfigDto>> GetBillingConfigAsync()
        {
            var config = new BillingConfigDto
            {
                ApiCallRate = await GetSettingDecimalAsync("ApiCallRate", 0.01m),
                SmsRate = await GetSettingDecimalAsync("SmsRate", 2.00m),
                PlatformCommissionRate = await GetSettingDecimalAsync("PlatformCommissionRate", 1.00m),
                VatPercentage = await GetSettingDecimalAsync("VatPercentage", 0m),
                SsclPercentage = await GetSettingDecimalAsync("SsclPercentage", 0m)
            };

            return ServiceResponse<BillingConfigDto>.SuccessResponse(config);
        }

        public async Task<ServiceResponse<bool>> UpdateBillingConfigAsync(BillingConfigDto config)
        {
            await SaveSettingAsync("ApiCallRate", config.ApiCallRate.ToString());
            await SaveSettingAsync("SmsRate", config.SmsRate.ToString());
            await SaveSettingAsync("PlatformCommissionRate", config.PlatformCommissionRate.ToString());
            await SaveSettingAsync("VatPercentage", config.VatPercentage.ToString());
            await SaveSettingAsync("SsclPercentage", config.SsclPercentage.ToString());

            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }

        public async Task<ServiceResponse<bool>> RolloverOverdueBillsAsync(int targetMonth, int targetYear)
        {
            var previousMonth = targetMonth == 1 ? 12 : targetMonth - 1;
            var previousYear = targetMonth == 1 ? targetYear - 1 : targetYear;

            // 1. Find all unpaid bills from the previous month
            var unpaidPreviousBills = await _context.Bills
                .Where(b => b.Month == previousMonth && b.Year == previousYear && b.Status == BillStatus.Unpaid.ToString())
                .ToListAsync();

            if (!unpaidPreviousBills.Any())
                return ServiceResponse<bool>.SuccessResponse(true, "No unpaid bills to rollover.");

            foreach (var prevBill in unpaidPreviousBills)
            {
                // 2. Mark previous bill as overdue
                prevBill.Status = BillStatus.Overdue.ToString();

                // 3. Find or create the bill for the target month
                var targetBill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.UserId == prevBill.UserId && b.Month == targetMonth && b.Year == targetYear && b.Status == BillStatus.Unpaid.ToString());

                if (targetBill == null)
                {
                    var startDateLkt = new DateTime(targetYear, targetMonth, 1);
                    var endDateLkt = startDateLkt.AddMonths(1).AddSeconds(-1);

                    var existingBillsCount = await _context.Bills.CountAsync(b => b.UserId == prevBill.UserId && b.Month == targetMonth && b.Year == targetYear);
                    var suffix = existingBillsCount > 0 ? $"-{(existingBillsCount + 1):D2}" : "";

                    targetBill = new Bill
                    {
                        UserId = prevBill.UserId,
                        Month = targetMonth,
                        Year = targetYear,
                        UserRole = prevBill.UserRole,
                        BillReference = $"TZ-{targetYear}-{targetMonth:D2}-{prevBill.UserId.ToString().Substring(0, 4).ToUpper()}{suffix}",
                        MonthYear = $"{targetYear}-{targetMonth:D2}",
                        BillStartDate = startDateLkt,
                        BillEndDate = endDateLkt,
                        GeneratedAt = DateTime.UtcNow,
                        Status = BillStatus.Unpaid.ToString(),
                        PreviousOverdueAmount = prevBill.PayableAmount
                    };
                    _context.Bills.Add(targetBill);
                }
                else
                {
                    targetBill.PreviousOverdueAmount += prevBill.PayableAmount;
                }

                await RecalculateBillTotalsAsync(targetBill);
            }

            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true, $"Rolled over {unpaidPreviousBills.Count} overdue bills.");
        }

        private async Task<Bill?> GetOrCreateBillAsync(Guid userId, int month, int year)
        {
            // Only find an active (Unpaid) bill for this month. 
            // If the user already paid their bill for this month, we create a new one.
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year && b.Status == BillStatus.Unpaid.ToString());

            if (bill == null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return null;

                var startDateLkt = new DateTime(year, month, 1);
                var endDateLkt = startDateLkt.AddMonths(1).AddSeconds(-1);

                var existingBillsCount = await _context.Bills.CountAsync(b => b.UserId == userId && b.Month == month && b.Year == year);
                var suffix = existingBillsCount > 0 ? $"-{(existingBillsCount + 1):D2}" : "";

                bill = new Bill
                {
                    UserId = userId,
                    Month = month,
                    Year = year,
                    UserRole = "Unknown",
                    BillReference = $"TZ-{year}-{month:D2}-{userId.ToString().Substring(0, 4).ToUpper()}{suffix}",
                    MonthYear = $"{year}-{month:D2}",
                    BillStartDate = startDateLkt,
                    BillEndDate = endDateLkt,
                    GeneratedAt = DateTime.UtcNow,
                    Status = BillStatus.Unpaid.ToString()
                };

                var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == userId);
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);
                if (institute != null) bill.UserRole = "Institute";
                else if (tutor != null) bill.UserRole = "Tutor";
                else bill.UserRole = "Student";

                var previousMonth = month == 1 ? 12 : month - 1;
                var previousYear = month == 1 ? year - 1 : year;
                var previousBill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == previousMonth && b.Year == previousYear);

                if (previousBill != null && (previousBill.Status == BillStatus.Unpaid.ToString() || previousBill.Status == BillStatus.Overdue.ToString()))
                {
                    bill.PreviousOverdueAmount = previousBill.PayableAmount;
                    if (previousBill.Status == BillStatus.Unpaid.ToString())
                    {
                        previousBill.Status = BillStatus.Overdue.ToString();
                    }
                }

                _context.Bills.Add(bill);
            }

            return bill;
        }

        private async Task RecalculateBillTotalsAsync(Bill bill)
        {
            var config = (await GetBillingConfigAsync()).Data;
            if (config == null) return;
            
            bill.SubTotal = bill.ApiUsageAmount + bill.SmsAmount + bill.PlatformCommissionAmount + bill.PreviousOverdueAmount;
            var taxPercentage = config.VatPercentage + config.SsclPercentage;
            bill.TaxPercentage = taxPercentage;
            bill.TaxAmount = Math.Round(bill.SubTotal * (taxPercentage / 100), 2);
            bill.PayableAmount = bill.SubTotal + bill.TaxAmount;
            bill.GeneratedAt = DateTime.UtcNow;
        }

        public async Task IncrementPlatformCommissionAsync(Guid instituteId, Guid tutorId, decimal instituteCommission, decimal tutorCommission, int month, int year)
        {
            var institute = await _context.Institutes.FindAsync(instituteId);
            if (institute != null && instituteCommission > 0)
            {
                var instituteBill = await GetOrCreateBillAsync(institute.UserId, month, year);
                if (instituteBill != null)
                {
                    instituteBill.PlatformCommissionAmount += instituteCommission;
                    await RecalculateBillTotalsAsync(instituteBill);
                }
            }

            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor != null && tutorCommission > 0)
            {
                var tutorBill = await GetOrCreateBillAsync(tutor.UserId, month, year);
                if (tutorBill != null)
                {
                    tutorBill.PlatformCommissionAmount += tutorCommission;
                    await RecalculateBillTotalsAsync(tutorBill);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task IncrementSmsUsageAsync(Guid userId, int smsCount, decimal smsCost, DateTime date)
        {
            var lktDate = TimeZoneInfo.ConvertTimeFromUtc(date, SriLankaTimeZone);
            var bill = await GetOrCreateBillAsync(userId, lktDate.Month, lktDate.Year);
            if (bill != null)
            {
                var config = (await GetBillingConfigAsync()).Data;
                if (config != null)
                {
                    bill.SmsRate = config.SmsRate;
                    bill.SmsSentCount += smsCount;
                    bill.SmsAmount += smsCost;
                    await RecalculateBillTotalsAsync(bill);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task IncrementApiUsageAsync(Guid userId, int apiCallCount, DateTime date)
        {
            var lktDate = TimeZoneInfo.ConvertTimeFromUtc(date, SriLankaTimeZone);
            var bill = await GetOrCreateBillAsync(userId, lktDate.Month, lktDate.Year);
            if (bill != null)
            {
                var config = (await GetBillingConfigAsync()).Data;
                if (config != null)
                {
                    bill.ApiCallRate = config.ApiCallRate;
                    bill.ApiCallCount += apiCallCount;
                    bill.ApiUsageAmount += (apiCallCount * config.ApiCallRate);
                    await RecalculateBillTotalsAsync(bill);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task<decimal> GetSettingDecimalAsync(string key, decimal defaultValue)
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null || !decimal.TryParse(setting.Value, out var val)) return defaultValue;
            return val;
        }

        private async Task SaveSettingAsync(string key, string value)
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new AppSetting { Key = key, UpdatedAt = DateTime.UtcNow };
                _context.AppSettings.Add(setting);
            }
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        public async Task<byte[]?> GenerateBillPdfAsync(Guid billId, Guid requestingUserId, string requestingRole)
        {
            var billResult = await GetBillByIdAsync(billId, requestingUserId, requestingRole);
            if (!billResult.Success || billResult.Data == null) return null;

            var data = billResult.Data;

            // Generate PDF using QuestPDF
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
                            col.Item().Text("Tutorz.lk").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Kylix Technology (Tutorz Team)");
                            col.Item().Text("lktutorz@gmail.com");
                            col.Item().Text("Sri Lanka");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("PLATFORM INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Bill #: {data.BillReference}");
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
                                c.Item().Text(data.UserName);
                                c.Item().Text(data.Email);
                                c.Item().Text($"Role: {data.UserRole}");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Billing Period:").Bold();
                                c.Item().Text($"{data.MonthYear}");
                                c.Item().Text($"{data.BillStartDate:dd MMM} - {data.BillEndDate:dd MMM yyyy}");
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Qty");
                                header.Cell().AlignRight().Text("Rate");
                                header.Cell().AlignRight().Text("Amount");

                                header.Cell().ColumnSpan(5).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            // API Calls
                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("API Service Usage");
                            table.Cell().AlignRight().Text($"{data.ApiCallCount}");
                            table.Cell().AlignRight().Text($"{data.ApiCallRate:N4}");
                            table.Cell().AlignRight().Text($"{data.ApiUsageAmount:N2}");

                            // SMS
                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("SMS Dispatch Service");
                            table.Cell().AlignRight().Text($"{data.SmsSentCount}");
                            table.Cell().AlignRight().Text($"{data.SmsRate:N2}");
                            table.Cell().AlignRight().Text($"{data.SmsAmount:N2}");

                            // Commissions Breakdown
                            foreach (var item in data.ClassCommissions)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text(item.ClassName);
                                table.Cell().AlignRight().Text($"{item.Earnings:N2}");
                                table.Cell().AlignRight().Text($"{item.Rate:N2}%");
                                table.Cell().AlignRight().Text($"{item.Amount:N2}");
                            }

                            // Fallback if no detailed items found but there is a total (for legacy bills)
                            if (data.ClassCommissions.Count == 0 && data.PlatformCommissionAmount > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Platform Commission (Total)");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text($"{data.PlatformCommissionAmount:N2}");
                            }

                            // Overdue
                            if (data.PreviousOverdueAmount > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Previous Overdue Balance");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text($"{data.PreviousOverdueAmount:N2}");
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(5).PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black);
                                
                                footer.Cell().ColumnSpan(4).AlignRight().Text("Sub Total").Bold();
                                footer.Cell().AlignRight().Text($"{data.SubTotal:N2}");

                                footer.Cell().ColumnSpan(4).AlignRight().Text($"Tax ({data.TaxPercentage}%)").Bold();
                                footer.Cell().AlignRight().Text($"{data.TaxAmount:N2}");

                                footer.Cell().ColumnSpan(4).AlignRight().PaddingTop(5).Text("TOTAL PAYABLE (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5).Text($"{data.PayableAmount:N2}").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text("Status: " + data.Status.ToUpper()).Bold().FontColor(data.Status == "Paid" ? Colors.Green.Medium : Colors.Red.Medium);
                            c.Item().Text("Payment Terms: Please settle this invoice within 15 days.");
                            c.Item().PaddingTop(10).Text("Note: This is a system-generated invoice for platform usage fees.").Italic().FontSize(8);
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
