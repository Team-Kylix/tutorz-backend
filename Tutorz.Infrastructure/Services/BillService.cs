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
                query = query.Where(b => 
                    b.User.Email.Contains(search) || 
                    b.BillReference.Contains(search) ||
                    (b.User.RegistrationNumber != null && b.User.RegistrationNumber.Contains(search)) ||
                    (b.User.PhoneNumber != null && b.User.PhoneNumber.Contains(search)) ||
                    (b.UserRole == "Student" && _context.Students.Any(s => s.UserId == b.UserId && (s.FirstName + " " + s.LastName).Contains(search))) ||
                    (b.UserRole == "Tutor" && _context.Tutors.Any(t => t.UserId == b.UserId && (t.FirstName + " " + t.LastName).Contains(search))) ||
                    (b.UserRole == "Institute" && _context.Institutes.Any(i => i.UserId == b.UserId && i.InstituteName.Contains(search)))
                );
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
                Address = bill.UserRole == "Tutor"
                    ? await _context.Tutors.Where(t => t.UserId == bill.UserId).Select(t => t.Address).FirstOrDefaultAsync()
                    : bill.UserRole == "Institute"
                        ? await _context.Institutes.Where(i => i.UserId == bill.UserId).Select(i => i.Address).FirstOrDefaultAsync()
                        : null,
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

            bool isBillPaid = bill.Status == BillStatus.Paid.ToString();

            // Convert BillStartDate to UTC safely (stored as LKT, unspecified kind)
            var startUtc = DateTime.SpecifyKind(bill.BillStartDate, DateTimeKind.Unspecified)
                .Subtract(TimeSpan.FromHours(5.5)); // LKT = UTC+5:30

            // ── KEY FIX: bill.UserId is the AspNetUsers UserId.
            // ClassPayment.InstituteId  = Institute.InstituteId  (NOT UserId)
            // Class.TutorId             = Tutor.TutorId          (NOT UserId)
            // We must resolve the correct entity ID first.
            IQueryable<ClassPayment> paymentQuery = _context.ClassPayments
                .Include(p => p.Class)
                    .ThenInclude(c => c.Institute)
                .Include(p => p.Class)
                    .ThenInclude(c => c.Tutor)
                // Always filter from BillStartDate so only this billing window's payments appear
                .Where(p => p.Status == "Paid" && p.PaidAt >= startUtc);

            // For paid bills also cap at the PaidAt timestamp (end of window)
            if (isBillPaid && bill.PaidAt.HasValue)
            {
                var endUtc = bill.PaidAt.Value;
                paymentQuery = paymentQuery.Where(p => p.PaidAt <= endUtc);
            }

            if (bill.UserRole == "Tutor")
            {
                // Resolve TutorId from UserId
                var tutorId = await _context.Tutors
                    .Where(t => t.UserId == bill.UserId)
                    .Select(t => t.TutorId)
                    .FirstOrDefaultAsync();

                paymentQuery = tutorId == Guid.Empty
                    ? paymentQuery.Where(p => false)
                    : paymentQuery.Where(p => p.Class.TutorId == tutorId);
            }
            else if (bill.UserRole == "Institute")
            {
                // Resolve InstituteId from UserId
                var instituteId = await _context.Institutes
                    .Where(i => i.UserId == bill.UserId)
                    .Select(i => i.InstituteId)
                    .FirstOrDefaultAsync();

                paymentQuery = instituteId == Guid.Empty
                    ? paymentQuery.Where(p => false)
                    : paymentQuery.Where(p => p.InstituteId == instituteId);
            }
            else
            {
                // Students don't have platform commission breakdowns
                paymentQuery = paymentQuery.Where(p => false);
            }

            // Only include payments where the commission was actually calculated and recorded.
            // Payments with NULL TutorCommission/InstituteCommission were NOT accumulated into
            // the bill total — including them causes line items to not match the Sub Total.
            if (bill.UserRole == "Tutor")
                paymentQuery = paymentQuery.Where(p => p.TutorCommission != null);
            else if (bill.UserRole == "Institute")
                paymentQuery = paymentQuery.Where(p => p.InstituteCommission != null);

            var payments = await paymentQuery.ToListAsync();

            // Pre-calculate the formatted name to group identical display strings together
            var paymentsWithNames = payments.Select(p => {
                var cls = p.Class;
                string monthStr = $"{p.Year}-{p.Month:D2}";
                string instName = cls?.Institute?.InstituteName;
                string baseName = $"{cls?.Grade} {cls?.Subject}".Trim();
                
                if (!string.IsNullOrEmpty(instName)) 
                {
                    baseName += $" - {instName}";
                }
                
                if (string.IsNullOrWhiteSpace(baseName)) 
                {
                    baseName = cls?.ClassName ?? "Unknown Class";
                }

                return new {
                    Payment = p,
                    FormattedName = $"{baseName} {monthStr}".Trim(),
                    TutorName = cls?.Tutor != null ? $"{cls.Tutor.FirstName} {cls.Tutor.LastName}".Trim() : "Unknown Tutor"
                };
            }).ToList();

            dto.ClassCommissions = paymentsWithNames
                .GroupBy(x => new { x.FormattedName, x.TutorName })
                .Select(g => {
                    decimal earnings = bill.UserRole == "Tutor"
                        ? g.Sum(x => x.Payment.TuitionAmount ?? 0)
                        : g.Sum(x => x.Payment.InstituteAmount ?? 0);

                    decimal commission = bill.UserRole == "Tutor"
                        ? g.Sum(x => x.Payment.TutorCommission ?? 0)
                        : g.Sum(x => x.Payment.InstituteCommission ?? 0);

                    return new ClassCommissionItemDto
                    {
                        ClassName = g.Key.FormattedName,
                        TutorName = g.Key.TutorName,
                        Earnings  = Math.Round(earnings, 2),
                        Rate      = dto.PlatformCommissionRate,
                        Amount    = Math.Round(commission, 2)
                    };
                })
                .Where(i => i.Amount > 0)
                .OrderBy(i => i.TutorName).ThenBy(i => i.ClassName)
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
            var nowLkt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SriLankaTimeZone);
            int updatedCount = 0;
            
            foreach (var b in bills)
            {
                bool modified = false;

                // Move unpaid bills to current month
                if (b.Status == BillStatus.Unpaid.ToString())
                {
                    if (b.Month != nowLkt.Month || b.Year != nowLkt.Year)
                    {
                        b.Month = nowLkt.Month;
                        b.Year = nowLkt.Year;
                        b.MonthYear = $"{nowLkt.Year}-{nowLkt.Month:D2}";
                        modified = true;
                    }
                }

                var refPart = b.UserId.ToString().Substring(0, 4).ToUpper();
                var newRef = $"TZ{b.Year % 100:D2}{b.Month:D2}{refPart}";
                
                if (b.BillReference != newRef) 
                {
                    b.BillReference = newRef;
                    modified = true;
                }

                if (modified) updatedCount++;
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
                        BillReference = $"TZ{targetYear % 100:D2}{targetMonth:D2}{prevBill.UserId.ToString().Substring(0, 4).ToUpper()}{suffix}",
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
            // ROLLING BILL MODEL:
            // There is only ever ONE open (Unpaid) bill per user at a time.
            // When a class payment arrives, we always add it to the single open bill.
            // A new bill is only created when the previous one was paid (cleared).
            // The month/year parameters are kept for API compatibility but are no longer
            // used to enforce month-based bucketing.

            // 1. Find the single open (Unpaid) bill for this user, regardless of month
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Status == BillStatus.Unpaid.ToString());

            if (bill != null)
            {
                // Update the month/year to the latest payment's period so the reference stays current
                if (bill.Month != month || bill.Year != year)
                {
                    bill.Month = month;
                    bill.Year = year;
                    bill.MonthYear = $"{year}-{month:D2}";
                    
                    var existingCount = await _context.Bills.CountAsync(b => b.UserId == userId && b.Month == month && b.Year == year && b.BillId != bill.BillId);
                    var suff = existingCount > 0 ? $"-{(existingCount + 1):D2}" : "";
                    bill.BillReference = $"TZ{year % 100:D2}{month:D2}{userId.ToString().Substring(0, 4).ToUpper()}{suff}";
                }
                return bill;
            }

            // 2. No open bill â€” create a fresh one.
            // Start date = the PaidAt of the last paid bill (rolling window).
            // If this is the user's very first bill, start from today.
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var lastPaidBill = await _context.Bills
                .Where(b => b.UserId == userId && b.Status == BillStatus.Paid.ToString())
                .OrderByDescending(b => b.PaidAt)
                .FirstOrDefaultAsync();

            // BillStartDate = when the last bill was paid (LKT), or now if first bill
            var nowLkt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SriLankaTimeZone);
            DateTime startDateLkt = lastPaidBill?.PaidAt != null
                ? TimeZoneInfo.ConvertTimeFromUtc(lastPaidBill.PaidAt.Value, SriLankaTimeZone)
                : nowLkt;

            // BillEndDate is dynamic (always recalculated at view time), store a placeholder
            DateTime endDateLkt = nowLkt;

            // Bill reference uses the current payment's month/year for readability
            var existingBillsCount = await _context.Bills.CountAsync(b => b.UserId == userId && b.Month == month && b.Year == year);
            var suffix = existingBillsCount > 0 ? $"-{(existingBillsCount + 1):D2}" : "";

            bill = new Bill
            {
                UserId = userId,
                Month = month,
                Year = year,
                UserRole = "Unknown",
                BillReference = $"TZ{year % 100:D2}{month:D2}{userId.ToString().Substring(0, 4).ToUpper()}{suffix}",
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

            _context.Bills.Add(bill);
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

            // Logo â€” falls back to "Tutorz.lk" blue text if image not found
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FullLogo.png");
            bool hasLogo = File.Exists(logoPath);
            bool isPaid = data.Status == "Paid";

            // ── Compute totals from the DISPLAYED line items so the footer always matches ──
            // Commission = sum of all per-class commission amounts shown in the table
            decimal pdfCommissionTotal = data.ClassCommissions.Sum(i => i.Amount);

            // If no per-class breakdown, fall back to the stored PlatformCommissionAmount
            if (pdfCommissionTotal == 0 && data.PlatformCommissionAmount > 0)
                pdfCommissionTotal = data.PlatformCommissionAmount;

            decimal pdfSubTotal    = pdfCommissionTotal + data.ApiUsageAmount + data.SmsAmount + data.PreviousOverdueAmount;
            decimal pdfTaxAmount   = Math.Round(pdfSubTotal * (data.TaxPercentage / 100m), 2);
            decimal pdfTotalPayable = pdfSubTotal + pdfTaxAmount;

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
                            col.Item().Text("PLATFORM INVOICE").FontSize(20).Bold();
                            col.Item().Text($"Bill #: {data.BillReference}");
                            col.Item().Text($"Date: {data.GeneratedAt:dd MMM yyyy}");
                        });
                    });

                    // â”€â”€ CONTENT â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        // Billed To / Billing Period
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(data.UserName);
                                if (!string.IsNullOrWhiteSpace(data.RegistrationNumber))
                                    c.Item().Text(data.RegistrationNumber);
                                if (!string.IsNullOrWhiteSpace(data.Address))
                                    c.Item().Text(data.Address);
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
                                columns.ConstantColumn(20);   // #
                                columns.RelativeColumn();     // Description
                                columns.ConstantColumn(60);   // Qty / Earnings
                                columns.ConstantColumn(80);   // Rate
                                columns.ConstantColumn(80);   // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#");
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Collection");
                                header.Cell().AlignRight().Text("Rate");
                                header.Cell().AlignRight().Text("Charge");

                                // Single bottom border line under header (original style)
                                header.Cell().ColumnSpan(5).PaddingVertical(5)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int rowNum = 1;

                            // â”€â”€ Per-class commission rows â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                            if (data.UserRole == "Institute")
                            {
                                var groupedByTutor = data.ClassCommissions.GroupBy(c => c.TutorName ?? "Unknown Tutor").OrderBy(g => g.Key);
                                foreach (var tutorGroup in groupedByTutor)
                                {
                                    // Group Header
                                    table.Cell().ColumnSpan(5).PaddingTop(5).PaddingBottom(2).Text(tutorGroup.Key).Bold();

                                    foreach (var item in tutorGroup)
                                    {
                                        table.Cell().Text($"{rowNum++}");
                                        table.Cell().PaddingLeft(10).Text($"{item.ClassName}");
                                        table.Cell().AlignRight().Text($"{item.Earnings:N2}");
                                        table.Cell().AlignRight().Text($"{item.Rate:N2}%");
                                        table.Cell().AlignRight().Text($"{item.Amount:N2}");
                                    }
                                }
                            }
                            else
                            {
                                foreach (var item in data.ClassCommissions)
                                {
                                    table.Cell().Text($"{rowNum++}");
                                    table.Cell().Text($"{item.ClassName}");
                                    table.Cell().AlignRight().Text($"{item.Earnings:N2}");
                                    table.Cell().AlignRight().Text($"{item.Rate:N2}%");
                                    table.Cell().AlignRight().Text($"{item.Amount:N2}");
                                }
                            }

                            // Fallback for legacy bills with no per-class detail
                            if (data.ClassCommissions.Count == 0 && data.PlatformCommissionAmount > 0)
                            {
                                table.Cell().Text($"{rowNum++}");
                                table.Cell().Text("Platform Commission (Total)");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text("-");
                                table.Cell().AlignRight().Text($"{data.PlatformCommissionAmount:N2}");
                            }

                            // API Calls (always shown, matching original screenshot)
                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("API Service Usage");
                            table.Cell().AlignRight().Text($"{data.ApiCallCount}");
                            table.Cell().AlignRight().Text($"{data.ApiCallRate:N4}");
                            table.Cell().AlignRight().Text($"{data.ApiUsageAmount:N2}");

                            // SMS (always shown, matching original screenshot)
                            table.Cell().Text($"{rowNum++}");
                            table.Cell().Text("SMS Dispatch Service");
                            table.Cell().AlignRight().Text($"{data.SmsSentCount}");
                            table.Cell().AlignRight().Text($"{data.SmsRate:N2}");
                            table.Cell().AlignRight().Text($"{data.SmsAmount:N2}");

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
                                footer.Cell().ColumnSpan(5).PaddingVertical(5)
                                    .BorderTop(1).BorderColor(Colors.Black);

                                footer.Cell().ColumnSpan(4).AlignRight().Text("Sub Total").Bold();
                                footer.Cell().AlignRight().Text($"{pdfSubTotal:N2}");

                                footer.Cell().ColumnSpan(4).AlignRight()
                                    .Text($"Tax ({data.TaxPercentage:N2}%)").Bold();
                                footer.Cell().AlignRight().Text($"{pdfTaxAmount:N2}");

                                footer.Cell().ColumnSpan(4).AlignRight().PaddingTop(5)
                                    .Text("TOTAL PAYABLE (LKR)").FontSize(14).Bold();
                                footer.Cell().AlignRight().PaddingTop(5)
                                    .Text($"{pdfTotalPayable:N2}").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(40).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Status: ").Bold();
                                text.Span(data.Status.ToUpper()).Bold()
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
