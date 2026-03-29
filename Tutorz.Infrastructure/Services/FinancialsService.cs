using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Financials;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// Handles saving and retrieving financial details (bank accounts + card tokens).
    ///
    /// Security model:
    ///   - All bank sensitive fields (account number, holder name, bank name, branch name)
    ///     are encrypted with AES-256 before being written to the DB.
    ///   - The DB stores only Base64 ciphertext — devs cannot read real values.
    ///   - BankCode and BranchCode are stored in plaintext (needed for UI dropdown re-selection;
    ///     these are public LankaPay codes — not secret).
    ///   - MaskedAccountNumber is computed and saved at write-time so the UI never needs to
    ///     decrypt anything just to display "****  5678".
    ///   - Card tokens (PayHere mock) are stored as-is; they are opaque tokens, not card numbers.
    ///   - GetFinancialSummaryAsync NEVER decrypts account numbers back — it only returns
    ///     the pre-computed masked copy.
    /// </summary>
    public class FinancialsService : IFinancialsService
    {
        private readonly TutorzDbContext _context;
        private readonly IEncryptionService _enc;

        public FinancialsService(TutorzDbContext context, IEncryptionService enc)
        {
            _context = context;
            _enc = enc;
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
                    tutor.PayHereToken    = dto.Token;
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
                    institute.PayHereToken   = dto.Token;
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
                    student.PayHereToken   = dto.Token;
                    student.CardLast4      = dto.Last4;
                    student.CardBrand      = dto.Brand;
                    student.CardholderName = dto.CardholderName;
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
                        CardholderName = student.CardholderName
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
        //  Private Helpers
        // ─────────────────────────────────────────────

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

        /// <summary>
        /// Decrypts a ciphertext, returning null (instead of throwing) if the value is
        /// null/empty or if decryption fails (e.g. wrong key, legacy plain-text value).
        /// </summary>
        private string? SafeDecrypt(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;
            var result = _enc.Decrypt(cipherText);
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private static void ClearBankFields(Tutorz.Domain.Entities.Tutor t)
        {
            t.EncryptedBankName = null;
            t.EncryptedBranchName = null;
            t.EncryptedAccountNumber = null;
            t.EncryptedAccountHolderName = null;
            t.BankCode = null;
            t.BranchCode = null;
            t.MaskedAccountNumber = null;
        }

        private static void ClearBankFields(Tutorz.Domain.Entities.Institute i)
        {
            i.EncryptedBankName = null;
            i.EncryptedBranchName = null;
            i.EncryptedAccountNumber = null;
            i.EncryptedAccountHolderName = null;
            i.BankCode = null;
            i.BranchCode = null;
            i.MaskedAccountNumber = null;
        }

        // ─── Result builders ───
        private static ServiceResponse<T> Ok<T>(T data, string message = "") =>
            new() { Success = true, Data = data, Message = message };

        private static ServiceResponse<T> Fail<T>(string message) =>
            new() { Success = false, Message = message };
    }
}
