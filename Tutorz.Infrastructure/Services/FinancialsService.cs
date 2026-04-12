using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Financials;
using Tutorz.Application.DTOs.Payment;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// Handles saving and retrieving financial details (bank accounts + PayHere card tokens).
    ///
    /// Security model:
    ///   - All bank sensitive fields (account number, holder name, bank name, branch name)
    ///     are encrypted with AES-256 before being written to the DB.
    ///   - The DB stores only Base64 ciphertext — devs cannot read real values.
    ///   - BankCode and BranchCode are stored in plaintext (public LankaPay codes — not secret).
    ///   - MaskedAccountNumber is computed and saved at write-time so the UI never needs to
    ///     decrypt anything just to display "****  5678".
    ///   - Card tokens are real PayHere customer_tokens (encrypted on PayHere's side) stored as-is.
    ///   - GetFinancialSummaryAsync NEVER decrypts account numbers back — it only returns
    ///     the pre-computed masked copy.
    /// </summary>
    public class FinancialsService : IFinancialsService
    {
        private readonly TutorzDbContext _context;
        private readonly IEncryptionService _enc;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        // PayHere config — loaded from appsettings PayHere section
        private string PayHereBaseUrl => _config["PayHere:BaseUrl"] ?? "https://sandbox.payhere.lk";
        private string PayHereMerchantId => _config["PayHere:MerchantId"] ?? "1234927";
        private string PayHereMerchantSecret => _config["PayHere:MerchantSecret"] ?? string.Empty;
        private string PayHereAppId => _config["PayHere:AppId"] ?? string.Empty;
        private string PayHereAppSecret => _config["PayHere:AppSecret"] ?? string.Empty;
        private string PayHereNotifyUrl => _config["PayHere:NotifyUrl"] ?? string.Empty;

        public FinancialsService(
            TutorzDbContext context,
            IEncryptionService enc,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _enc = enc;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // ─────────────────────────────────────────────
        //  BANK DETAILS
        // ─────────────────────────────────────────────

        public async Task<ServiceResponse<FinancialSummaryDto>> SaveBankDetailsAsync(
            Guid ownerId, string role, SaveBankDetailsDto dto)
        {
            switch (role.ToLower())
            {
                case "tutor":
                {
                    var tutor = await _context.Tutors
                        .FirstOrDefaultAsync(t => t.TutorId == ownerId || t.UserId == ownerId);
                    if (tutor == null)
                        return Fail<FinancialSummaryDto>("Tutor not found.");

                    tutor.BankCode                 = dto.BankCode;
                    tutor.BranchCode               = dto.BranchCode;
                    tutor.EncryptedBankName         = _enc.Encrypt(dto.BankName);
                    tutor.EncryptedBranchName       = _enc.Encrypt(dto.BranchName);
                    tutor.EncryptedAccountNumber    = _enc.Encrypt(dto.AccountNumber);
                    tutor.EncryptedAccountHolderName = _enc.Encrypt(dto.AccountHolderName);
                    tutor.MaskedAccountNumber       = _enc.Mask(dto.AccountNumber);
                    break;
                }

                case "institute":
                {
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.InstituteId == ownerId || i.UserId == ownerId);
                    if (institute == null)
                        return Fail<FinancialSummaryDto>("Institute not found.");

                    institute.BankCode                  = dto.BankCode;
                    institute.BranchCode                = dto.BranchCode;
                    institute.EncryptedBankName          = _enc.Encrypt(dto.BankName);
                    institute.EncryptedBranchName        = _enc.Encrypt(dto.BranchName);
                    institute.EncryptedAccountNumber     = _enc.Encrypt(dto.AccountNumber);
                    institute.EncryptedAccountHolderName = _enc.Encrypt(dto.AccountHolderName);
                    institute.MaskedAccountNumber        = _enc.Mask(dto.AccountNumber);
                    break;
                }

                default:
                    return Fail<FinancialSummaryDto>("Bank details are not supported for this role.");
            }

            await _context.SaveChangesAsync();
            return await GetFinancialSummaryAsync(ownerId, role);
        }

        public async Task<ServiceResponse<bool>> RemoveBankDetailsAsync(Guid ownerId, string role)
        {
            switch (role.ToLower())
            {
                case "tutor":
                {
                    var tutor = await _context.Tutors
                        .FirstOrDefaultAsync(t => t.TutorId == ownerId || t.UserId == ownerId);
                    if (tutor == null) return Fail<bool>("Tutor not found.");
                    ClearBankFields(tutor);
                    break;
                }

                case "institute":
                {
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.InstituteId == ownerId || i.UserId == ownerId);
                    if (institute == null) return Fail<bool>("Institute not found.");
                    ClearBankFields(institute);
                    break;
                }

                default:
                    return Fail<bool>("Bank details are not supported for this role.");
            }

            await _context.SaveChangesAsync();
            return Ok(true, "Bank details removed.");
        }

        // ─────────────────────────────────────────────
        //  CARD TOKEN
        // ─────────────────────────────────────────────

        public async Task<ServiceResponse<FinancialSummaryDto>> SaveCardTokenAsync(
            Guid ownerId, string role, SaveCardTokenDto dto)
        {
            switch (role.ToLower())
            {
                case "tutor":
                {
                    var tutor = await _context.Tutors
                        .FirstOrDefaultAsync(t => t.TutorId == ownerId || t.UserId == ownerId);
                    if (tutor == null) return Fail<FinancialSummaryDto>("Tutor not found.");
                    tutor.PayHereToken    = _enc.Encrypt(dto.Token);
                    tutor.CardLast4       = dto.Last4;
                    tutor.CardBrand       = dto.Brand;
                    tutor.CardholderName  = dto.CardholderName;
                    break;
                }
                case "institute":
                {
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.InstituteId == ownerId || i.UserId == ownerId);
                    if (institute == null) return Fail<FinancialSummaryDto>("Institute not found.");
                    institute.PayHereToken   = _enc.Encrypt(dto.Token);
                    institute.CardLast4      = dto.Last4;
                    institute.CardBrand      = dto.Brand;
                    institute.CardholderName = dto.CardholderName;
                    break;
                }
                case "student":
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.StudentId == ownerId || s.UserId == ownerId);
                    if (student == null) return Fail<FinancialSummaryDto>("Student not found.");
                    student.PayHereToken   = _enc.Encrypt(dto.Token);
                    student.CardLast4      = dto.Last4;
                    student.CardBrand      = dto.Brand;
                    student.CardholderName = dto.CardholderName;
                    student.CardExpiry     = dto.CardExpiry;
                    break;
                }
                default:
                    return Fail<FinancialSummaryDto>("Unknown role.");
            }

            await _context.SaveChangesAsync();
            return await GetFinancialSummaryAsync(ownerId, role);
        }

        public async Task<ServiceResponse<bool>> RemoveCardTokenAsync(Guid ownerId, string role)
        {
            switch (role.ToLower())
            {
                case "tutor":
                {
                    var tutor = await _context.Tutors
                        .FirstOrDefaultAsync(t => t.TutorId == ownerId || t.UserId == ownerId);
                    if (tutor == null) return Fail<bool>("Tutor not found.");
                    tutor.PayHereToken = null; tutor.CardLast4 = null;
                    tutor.CardBrand = null; tutor.CardholderName = null;
                    break;
                }
                case "institute":
                {
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.InstituteId == ownerId || i.UserId == ownerId);
                    if (institute == null) return Fail<bool>("Institute not found.");
                    institute.PayHereToken = null; institute.CardLast4 = null;
                    institute.CardBrand = null; institute.CardholderName = null;
                    break;
                }
                case "student":
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.StudentId == ownerId || s.UserId == ownerId);
                    if (student == null) return Fail<bool>("Student not found.");
                    student.PayHereToken = null; student.CardLast4 = null;
                    student.CardBrand = null; student.CardholderName = null;
                    student.CardExpiry = null;
                    break;
                }
                default:
                    return Fail<bool>("Unknown role.");
            }

            await _context.SaveChangesAsync();
            return Ok(true, "Card removed.");
        }

        // ─────────────────────────────────────────────
        //  SUMMARY (masked only — no decryption of account number)
        // ─────────────────────────────────────────────

        public async Task<ServiceResponse<FinancialSummaryDto>> GetFinancialSummaryAsync(
            Guid ownerId, string role)
        {
            FinancialSummaryDto summary;

            switch (role.ToLower())
            {
                case "tutor":
                {
                    var tutor = await _context.Tutors
                        .FirstOrDefaultAsync(t => t.TutorId == ownerId || t.UserId == ownerId);
                    if (tutor == null) return Fail<FinancialSummaryDto>("Tutor not found.");
                    summary = BuildTutorSummary(tutor);
                    break;
                }
                case "institute":
                {
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.InstituteId == ownerId || i.UserId == ownerId);
                    if (institute == null) return Fail<FinancialSummaryDto>("Institute not found.");
                    summary = BuildInstituteSummary(institute);
                    break;
                }
                case "student":
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.StudentId == ownerId || s.UserId == ownerId);
                    if (student == null) return Fail<FinancialSummaryDto>("Student not found.");
                    summary = new FinancialSummaryDto
                    {
                        HasBankDetails = false,
                        HasCard        = student.PayHereToken != null,
                        CardLast4      = student.CardLast4,
                        CardBrand      = student.CardBrand,
                        CardholderName = student.CardholderName,
                        CardExpiry     = student.CardExpiry
                    };
                    break;
                }
                default:
                    return Fail<FinancialSummaryDto>("Unknown role.");
            }

            return Ok(summary);
        }

        // ─────────────────────────────────────────────
        //  BANK DIRECTORY
        // ─────────────────────────────────────────────

        public async Task<ServiceResponse<IEnumerable<BankDto>>> GetBanksAsync()
        {
            var banks = await _context.Banks
                .OrderBy(b => b.BankName)
                .Select(b => new BankDto { BankCode = b.BankCode, BankName = b.BankName })
                .ToListAsync();

            return Ok<IEnumerable<BankDto>>(banks);
        }

        public async Task<ServiceResponse<IEnumerable<BranchDto>>> GetBranchesByBankAsync(int bankCode)
        {
            var branches = await _context.Branches
                .Where(b => b.BankCode == bankCode)
                .OrderBy(b => b.BranchName)
                .Select(b => new BranchDto
                {
                    BranchId   = b.BranchId,
                    BankCode   = b.BankCode,
                    BranchCode = b.BranchCode,
                    BranchName = b.BranchName,
                    District   = b.District
                })
                .ToListAsync();

            return Ok<IEnumerable<BranchDto>>(branches);
        }

        // ─────────────────────────────────────────────
        //  PAYHERE PREAPPROVAL (TOKENIZATION)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Builds the parameters required by the PayHere JS SDK preapproval popup.
        /// The frontend calls this to get a valid hash, then opens window.payhere.startPayment().
        /// </summary>
        public async Task<ServiceResponse<object>> InitiatePreapprovalAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return Fail<object>("Student not found.");

            string orderId  = $"TZ-PRE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            // PayHere minimum is LKR 30. We charge 30 to the student as a card registration fee.
            // Tutorz retains LKR 29; PayHere retains ~LKR 1 as their processing fee.
            string amount   = "30.00";
            string currency = "LKR";

            string hash = GenerateInitiationHash(PayHereMerchantId, orderId, amount, currency, PayHereMerchantSecret);

            return Ok<object>(new
            {
                sandbox         = true,
                preapprove      = true,
                merchant_id     = PayHereMerchantId,
                return_url      = "https://tutorz.lk/dashboard",
                cancel_url      = "https://tutorz.lk/dashboard",
                notify_url      = $"{PayHereNotifyUrl}/api/financials/preapproval-notify",
                order_id        = orderId,
                items           = "Card Registration Fee — Tutorz",
                currency        = currency,
                amount          = amount,
                hash            = hash,
                first_name      = student.FirstName,
                last_name       = student.LastName,
                email           = student.User?.Email ?? "student@tutorz.com",
                phone           = student.User?.PhoneNumber ?? "0771234567",
                address         = student.Address ?? "Sri Lanka",
                city            = "Colombo",
                country         = "Sri Lanka",
                custom_1        = studentId.ToString() // So the notify_url knows which student to update
            });
        }

        /// <summary>
        /// PayHere calls this endpoint after a successful preapproval.
        /// We verify the hash, then store the customer_token on the student record.
        /// </summary>
        public async Task<ServiceResponse<bool>> ProcessPreapprovalNotifyAsync(PreapprovalNotifyDto notify)
        {
            // Verify md5sig to ensure the request is genuinely from PayHere
            string localHash = GenerateNotificationHash(
                notify.merchant_id ?? "", notify.order_id ?? "",
                notify.payhere_amount ?? "", notify.payhere_currency ?? "",
                notify.status_code ?? "", PayHereMerchantSecret);

            if (!string.Equals(localHash, notify.md5sig, StringComparison.OrdinalIgnoreCase))
                return Fail<bool>("Invalid hash signature — preapproval notify rejected.");

            // status_code 2 = success
            if (notify.status_code != "2")
                return Fail<bool>($"Preapproval not successful (status_code={notify.status_code}).");

            if (string.IsNullOrWhiteSpace(notify.customer_token))
                return Fail<bool>("customer_token missing in preapproval notify.");

            // custom_1 holds the studentId we passed during initiation
            if (!Guid.TryParse(notify.custom_1, out var studentId))
                return Fail<bool>("Could not parse studentId from custom_1.");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return Fail<bool>("Student not found.");

            // Extract last4 from masked card_no (e.g. "************4242" => "4242")
            string last4 = notify.card_no?.Length >= 4
                ? notify.card_no.Substring(notify.card_no.Length - 4)
                : "";

            // Normalise brand: PayHere returns "VISA", "MASTER", etc.
            string brand = NormaliseBrand(notify.method);

            student.PayHereToken   = _enc.Encrypt(notify.customer_token ?? "");
            student.CardLast4      = last4;
            student.CardBrand      = brand;
            student.CardholderName = notify.card_holder_name;
            student.CardExpiry     = notify.card_expiry; // MMYY e.g. "0128"

            await _context.SaveChangesAsync();
            return Ok(true, "Card preapproval successful — token saved.");
        }

        // ─────────────────────────────────────────────
        //  ONLINE PAYMENTS (PAYHERE CHARGING API)
        // ─────────────────────────────────────────────

        public async Task<ServiceResponse<IEnumerable<MonthPaymentStatusDto>>> GetStudentPaymentStatusAsync(
            Guid classId, Guid studentId)
        {
            var classEntity = await _context.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == classId);
            if (classEntity == null) return Fail<IEnumerable<MonthPaymentStatusDto>>("Class not found.");

            var assignment = await _context.InstituteStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(is_ => is_.StudentId == studentId && is_.InstituteId == classEntity.InstituteId);

            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.ClassId == classId)
                .ToListAsync();

            var today = DateTime.UtcNow;
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            DateTime start = assignment != null
                ? new DateTime(assignment.AssignedDate.Year, assignment.AssignedDate.Month, 1)
                : currentMonth.AddMonths(-6);

            var end = currentMonth.AddMonths(3);
            var statuses = new List<MonthPaymentStatusDto>();
            var pointer = start;
            
            while (pointer <= end)
            {
                var m = pointer.Month;
                var y = pointer.Year;

                var paid = payments.Any(p => p.Month == m && p.Year == y &&
                                           (p.Status == "Paid" || p.Status == "PAID" || p.Status == "PENDING"));
                // Treat PENDING as paid to prevent double-payment while webhook is in flight

                string status;
                if (paid)
                    status = "Paid";
                else if (pointer > currentMonth)
                    status = "Future";
                else
                    status = "Unpaid";

                statuses.Add(new MonthPaymentStatusDto { Month = m, Year = y, Status = status });
                pointer = pointer.AddMonths(1);
            }

            return Ok<IEnumerable<MonthPaymentStatusDto>>(statuses);
        }

        public async Task<ServiceResponse<object>> InitiateOnlinePaymentAsync(
            Guid studentId, InitiatePaymentRequestDto request)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return Fail<object>("Student not found.");

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId);
            if (classEntity == null) return Fail<object>("Class not found.");

            // Verify amount
            decimal expectedTotal = CalculatePayHereTotal(classEntity.Fee);
            if (Math.Abs(request.Amount - expectedTotal) > 0.01m)
                return Fail<object>($"Incorrect payment amount. Expected LKR {expectedTotal:N2}.");

            var existingPayment = await _context.ClassPayments
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.ClassId == request.ClassId
                                         && p.Month == request.Month && p.Year == request.Year
                                         && p.Status == "Paid");
            if (existingPayment != null)
                return Fail<object>("This month is already paid.");

            string orderId = $"TZ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            string currency = "LKR";
            string formattedAmount = request.Amount.ToString("0.00");

            // ── ONE-CLICK PATH: Use PayHere Charging API ──────────────────────────────
            if (request.UseSavedCard && !string.IsNullOrEmpty(student.PayHereToken))
            {
                // 1. Obtain OAuth access token
                string? accessToken = await GetPayHereAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                    return Fail<object>("Could not obtain PayHere access token. Please try the standard checkout.");

                // 2. Call PayHere Charging API
                var chargeResult = await ChargeCustomerAsync(
                    accessToken,
                    orderId,
                    $"{classEntity.Subject} Fee - {request.Month}/{request.Year}",
                    currency,
                    request.Amount,
                    SafeDecrypt(student.PayHereToken) ?? "");

                if (chargeResult == null || chargeResult.StatusCode != 2)
                {
                    string errMsg = chargeResult?.Msg ?? "Payment failed. Please try standard checkout.";
                    return Fail<object>(errMsg);
                }

                // 3. Record the payment
                _context.ClassPayments.Add(new Tutorz.Domain.Entities.ClassPayment
                {
                    StudentId        = studentId,
                    ClassId          = request.ClassId,
                    InstituteId      = classEntity.InstituteId ?? Guid.Empty,
                    Month            = request.Month,
                    Year             = request.Year,
                    AmountPaid       = request.Amount,
                    Status           = "Paid",
                    ReferenceId      = orderId,
                    PaymentMethod    = student.CardBrand ?? "SAVED_CARD",
                    PayHerePaymentId = chargeResult.Data?.payment_id.ToString(),
                    PaidAt           = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return Ok<object>(new
                {
                    status     = "success",
                    message    = "Payment charged successfully using saved card.",
                    isAutoCharge = true
                });
            }

            // ── STANDARD CHECKOUT PATH ────────────────────────────────────────────────
            var payment = new Tutorz.Domain.Entities.ClassPayment
            {
                StudentId     = studentId,
                ClassId       = request.ClassId,
                InstituteId   = classEntity.InstituteId ?? Guid.Empty,
                Month         = request.Month,
                Year          = request.Year,
                AmountPaid    = request.Amount,
                Status        = "PENDING",
                ReferenceId   = orderId,
                PaymentMethod = "PAYHERE_CHECKOUT"
            };

            _context.ClassPayments.Add(payment);
            await _context.SaveChangesAsync();

            string hash = GenerateInitiationHash(PayHereMerchantId, orderId, formattedAmount, currency, PayHereMerchantSecret);

            return Ok<object>(new
            {
                sandbox      = true,
                merchant_id  = PayHereMerchantId,
                return_url   = "http://localhost:5173/dashboard/financials",
                cancel_url   = "http://localhost:5173/dashboard/financials",
                notify_url   = $"{PayHereNotifyUrl}/api/financials/payhere-notify",
                order_id     = orderId,
                items        = $"{classEntity.Subject} Fee - {request.Month}/{request.Year}",
                currency     = currency,
                amount       = formattedAmount,
                first_name   = student.FirstName,
                last_name    = student.LastName,
                email        = student.User?.Email ?? "student@tutorz.com",
                phone        = student.User?.PhoneNumber ?? "0771234567",
                address      = student.Address ?? "Sri Lanka",
                city         = "Colombo",
                country      = "Sri Lanka",
                hash         = hash
            });
        }

        public async Task<ServiceResponse<bool>> ProcessPayHereWebhookAsync(PayHereNotifyDto dto)
        {
            string localHash = GenerateNotificationHash(
                dto.merchant_id ?? "", dto.order_id ?? "",
                dto.payhere_amount ?? "", dto.payhere_currency ?? "",
                dto.status_code ?? "", PayHereMerchantSecret);

            if (!string.Equals(localHash, dto.md5sig, StringComparison.OrdinalIgnoreCase))
                return Fail<bool>("Invalid hash signature.");

            var payment = await _context.ClassPayments
                .FirstOrDefaultAsync(p => p.ReferenceId == dto.order_id);
            if (payment == null) return Fail<bool>("Order not found.");

            if (dto.status_code == "2") // SUCCESS
            {
                payment.Status           = "Paid";
                payment.PayHerePaymentId = dto.payment_id;
                payment.PaymentMethod    = dto.method;
                payment.PaidAt           = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(true, "Payment successful.");
            }
            else if (dto.status_code == "-1" || dto.status_code == "-2" || dto.status_code == "-3")
            {
                payment.Status = "FAILED";
                await _context.SaveChangesAsync();
                return Fail<bool>("Payment failed or cancelled.");
            }

            return Ok(true, "Ignored intermediate status.");
        }

        // ─────────────────────────────────────────────
        //  PAYHERE OAuth + Charging API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Retrieves a short-lived Bearer access token from PayHere's OAuth endpoint
        /// using the App ID + App Secret (Basic auth with base64 encoding).
        /// </summary>
        private async Task<string?> GetPayHereAccessTokenAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayHere");
                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{PayHereAppId}:{PayHereAppSecret}"));

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{PayHereBaseUrl}/merchant/v1/oauth/token");

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                });

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenEl))
                    return tokenEl.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Calls the PayHere Charging API to charge a preapproved customer using their token.
        /// Returns a typed response or null on failure.
        /// </summary>
        private async Task<PayHereChargeResponse?> ChargeCustomerAsync(
            string accessToken,
            string orderId,
            string items,
            string currency,
            decimal amount,
            string customerToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayHere");
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{PayHereBaseUrl}/merchant/v1/payment/charge");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        type           = "PAYMENT",
                        order_id       = orderId,
                        items          = items,
                        currency       = currency,
                        amount         = Math.Round(amount, 2),
                        customer_token = customerToken
                    }),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.SendAsync(request);
                var json     = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<PayHereChargeResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────
        //  Hash Generation
        // ─────────────────────────────────────────────

        private string GenerateInitiationHash(string merchantId, string orderId, string amount, string currency, string merchantSecret)
        {
            string hashedSecret = CreateMD5(merchantSecret).ToUpper();
            string data = merchantId + orderId + amount + currency + hashedSecret;
            return CreateMD5(data).ToUpper();
        }

        private string GenerateNotificationHash(string merchantId, string orderId, string amount, string currency, string statusCode, string merchantSecret)
        {
            string hashedSecret = CreateMD5(merchantSecret).ToUpper();
            string data = merchantId + orderId + amount + currency + statusCode + hashedSecret;
            return CreateMD5(data).ToUpper();
        }

        private string CreateMD5(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes  = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        // ─────────────────────────────────────────────
        //  Private Helpers
        // ─────────────────────────────────────────────

        private decimal CalculatePayHereTotal(decimal baseFee)
        {
            if (baseFee <= 0) return 0;
            return Math.Ceiling((baseFee + 30m) / 0.97m * 100m) / 100m;
        }

        /// <summary>Normalises PayHere method strings to friendly brand names.</summary>
        private static string NormaliseBrand(string method)
        {
            return method?.ToUpper() switch
            {
                "VISA"   => "Visa",
                "MASTER" => "Mastercard",
                "AMEX"   => "Amex",
                "DINERS" => "Diners",
                _        => method ?? "Card"
            };
        }

        private FinancialSummaryDto BuildTutorSummary(Tutorz.Domain.Entities.Tutor t)
        {
            bool hasBankDetails = t.EncryptedAccountNumber != null;
            return new FinancialSummaryDto
            {
                HasBankDetails      = hasBankDetails,
                MaskedAccountNumber = t.MaskedAccountNumber,
                BankName            = hasBankDetails ? SafeDecrypt(t.EncryptedBankName) : null,
                BranchName          = hasBankDetails ? SafeDecrypt(t.EncryptedBranchName) : null,
                AccountHolderName   = hasBankDetails ? SafeDecrypt(t.EncryptedAccountHolderName) : null,
                BankCode            = t.BankCode,
                BranchCode          = t.BranchCode,
                HasCard             = t.PayHereToken != null,
                CardLast4           = t.CardLast4,
                CardBrand           = t.CardBrand,
                CardholderName      = t.CardholderName
            };
        }

        private FinancialSummaryDto BuildInstituteSummary(Tutorz.Domain.Entities.Institute i)
        {
            bool hasBankDetails = i.EncryptedAccountNumber != null;
            return new FinancialSummaryDto
            {
                HasBankDetails      = hasBankDetails,
                MaskedAccountNumber = i.MaskedAccountNumber,
                BankName            = hasBankDetails ? SafeDecrypt(i.EncryptedBankName) : null,
                BranchName          = hasBankDetails ? SafeDecrypt(i.EncryptedBranchName) : null,
                AccountHolderName   = hasBankDetails ? SafeDecrypt(i.EncryptedAccountHolderName) : null,
                BankCode            = i.BankCode,
                BranchCode          = i.BranchCode,
                HasCard             = i.PayHereToken != null,
                CardLast4           = i.CardLast4,
                CardBrand           = i.CardBrand,
                CardholderName      = i.CardholderName
            };
        }

        private string? SafeDecrypt(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;
            var result = _enc.Decrypt(cipherText);
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private static void ClearBankFields(Tutorz.Domain.Entities.Tutor t)
        {
            t.EncryptedBankName = null; t.EncryptedBranchName = null;
            t.EncryptedAccountNumber = null; t.EncryptedAccountHolderName = null;
            t.BankCode = null; t.BranchCode = null; t.MaskedAccountNumber = null;
        }

        private static void ClearBankFields(Tutorz.Domain.Entities.Institute i)
        {
            i.EncryptedBankName = null; i.EncryptedBranchName = null;
            i.EncryptedAccountNumber = null; i.EncryptedAccountHolderName = null;
            i.BankCode = null; i.BranchCode = null; i.MaskedAccountNumber = null;
        }

        // ─── Result builders ───
        private static ServiceResponse<T> Ok<T>(T data, string message = "") =>
            new() { Success = true, Data = data, Message = message };

        private static ServiceResponse<T> Fail<T>(string message) =>
            new() { Success = false, Message = message };
    }

    // ─── PayHere Charging API response models ───
    internal class PayHereChargeResponse
    {
        public int Status { get; set; }
        public string? Msg { get; set; }
        public PayHereChargeData? Data { get; set; }
        // Convenience: status_code inside data.status_code
        public int StatusCode => Data?.status_code ?? Status;
    }

    internal class PayHereChargeData
    {
        public string? order_id { get; set; }
        public string? currency { get; set; }
        public decimal amount { get; set; }
        public long payment_id { get; set; }
        public int status_code { get; set; }
        public string? status_message { get; set; }
    }
}
