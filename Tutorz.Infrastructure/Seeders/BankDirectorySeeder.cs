using System;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Seeders
{
    /// <summary>
    /// Seeds Sri Lankan banks and branches from the LankaPay XLSX directory file.
    /// File expected at: wwwroot/LPPL_BankBranchDirectory_AllProducts_20260320.xlsx
    ///
    /// LankaPay file format (from user-provided column mapping):
    ///   Banks worksheet:
    ///     - Skip first 6 rows (LankaPay header)
    ///     - Col B (index 2) = BankCode, Col C (index 3) = BankName
    ///
    ///   Branches worksheet:
    ///     - Skip first 4 rows (LankaPay header)
    ///     - Col B (index 2) = BankCode, Col C (index 3) = BranchCode
    ///     - Col D (index 4) = BranchName, Col K (index 11) = District
    /// </summary>
    public class BankDirectorySeeder
    {
        private readonly TutorzDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BankDirectorySeeder> _logger;

        // File name inside wwwroot
        private const string XlsxFileName = "LPPL_BankBranchDirectory_AllProducts_20260320.xlsx";

        public BankDirectorySeeder(
            TutorzDbContext context,
            IWebHostEnvironment env,
            ILogger<BankDirectorySeeder> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Idempotent: skip if data already present
            if (await _context.Banks.AnyAsync())
            {
                _logger.LogInformation("BankDirectorySeeder: Bank data already present, skipping.");
                return;
            }

            var filePath = Path.Combine(_env.WebRootPath, XlsxFileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning(
                    "BankDirectorySeeder: File not found at {Path}. Bank directory NOT seeded.",
                    filePath);
                return;
            }

            _logger.LogInformation("BankDirectorySeeder: Seeding banks and branches from {File}", filePath);

            try
            {
                using var workbook = new XLWorkbook(filePath);

                // ── Sheet 1: Banks ──
                // The LankaPay file may have the bank list on the first sheet.
                // We auto-detect by looking for a sheet whose name contains "bank" (case-insensitive).
                var bankSheet = FindSheet(workbook, "bank") ?? workbook.Worksheet(1);
                var branchSheet = FindSheet(workbook, "branch") ?? workbook.Worksheet(2);

                var banks = new Dictionary<int, Bank>();

                // Skip first 6 header rows (LankaPay format)
                int bankStartRow = 7;
                int lastBankRow = bankSheet.LastRowUsed()?.RowNumber() ?? bankStartRow;

                for (int row = bankStartRow; row <= lastBankRow; row++)
                {
                    var codeCell = bankSheet.Cell(row, 2).Value; // Col B = BankCode
                    var nameCell = bankSheet.Cell(row, 3).Value; // Col C = BankName

                    if (!TryParseInt(codeCell, out int bankCode)) continue;
                    var bankName = nameCell.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(bankName)) continue;

                    if (!banks.ContainsKey(bankCode))
                    {
                        banks[bankCode] = new Bank { BankCode = bankCode, BankName = bankName };
                    }
                }

                if (banks.Count == 0)
                {
                    _logger.LogWarning("BankDirectorySeeder: No banks parsed from sheet. Check column mappings.");
                    return;
                }

                await _context.Banks.AddRangeAsync(banks.Values);
                await _context.SaveChangesAsync();
                _logger.LogInformation("BankDirectorySeeder: Seeded {Count} banks.", banks.Count);

                // ── Sheet 2: Branches ──
                // Skip first 4 header rows (LankaPay format)
                int branchStartRow = 5;
                int lastBranchRow = branchSheet.LastRowUsed()?.RowNumber() ?? branchStartRow;

                var branches = new List<Branch>();

                for (int row = branchStartRow; row <= lastBranchRow; row++)
                {
                    var bankCodeCell   = branchSheet.Cell(row, 2).Value;  // Col B = BankCode
                    var branchCodeCell = branchSheet.Cell(row, 3).Value;  // Col C = BranchCode
                    var branchNameCell = branchSheet.Cell(row, 4).Value;  // Col D = BranchName
                    var districtCell   = branchSheet.Cell(row, 11).Value; // Col K = District

                    if (!TryParseInt(bankCodeCell, out int bankCode)) continue;
                    if (!TryParseInt(branchCodeCell, out int branchCode)) continue;

                    var branchName = branchNameCell.ToString()?.Trim(' ', '"');
                    if (string.IsNullOrWhiteSpace(branchName)) continue;

                    // Only add if the parent bank exists (referential integrity)
                    if (!banks.ContainsKey(bankCode)) continue;

                    branches.Add(new Branch
                    {
                        BankCode    = bankCode,
                        BranchCode  = branchCode,
                        BranchName  = branchName,
                        District    = districtCell.ToString()?.Trim() ?? string.Empty
                    });
                }

                if (branches.Count > 0)
                {
                    await _context.Branches.AddRangeAsync(branches);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "BankDirectorySeeder: Seeded {Count} branches.", branches.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BankDirectorySeeder: Failed to seed bank directory.");
            }
        }

        // ── Helpers ──

        private static IXLWorksheet? FindSheet(XLWorkbook wb, string keyword)
        {
            foreach (var ws in wb.Worksheets)
            {
                if (ws.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return ws;
            }
            return null;
        }

        private static bool TryParseInt(XLCellValue value, out int result)
        {
            result = 0;
            if (value.IsBlank) return false;

            // Numeric cells come as double from ClosedXML
            if (value.IsNumber)
            {
                result = (int)value.GetNumber();
                return result > 0;
            }

            // Text cells
            return int.TryParse(value.ToString()?.Trim(), out result) && result > 0;
        }
    }
}
