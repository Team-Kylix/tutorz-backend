using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;

namespace Tutorz.Application.Services
{
    public class IdGeneratorService : IIdGeneratorService
    {
        private readonly IUserSequenceRepository _sequenceRepository;

        public IdGeneratorService(IUserSequenceRepository sequenceRepository)
        {
            _sequenceRepository = sequenceRepository;
        }

        public async Task<string> GenerateNextIdAsync(string roleName, string? gradeOrClass = null)
        {
            var now = DateTime.UtcNow;
            string year = now.ToString("yy");
            string month = now.Month.ToString();

            string rolePrefix = "";

            // Standard middle part: Year + Month (e.g., "2512")
            string middlePart = $"{year}{month}";

            switch (roleName.ToLower())
            {
                case "student":
                    rolePrefix = "STU";
                    // FIX: Removed 'classNum' from middlePart so the ID doesn't depend on the Grade.
                    // Old: middlePart = $"{year}{month}{classNum}";
                    break;
                case "tutor":
                    rolePrefix = "TUT";
                    break;
                case "institute":
                    rolePrefix = "INS";
                    break;
                case "admin":
                    rolePrefix = "ADM";
                    break;
                default:
                    throw new Exception("Unknown Role for ID generation");
            }

            // Generate Key (e.g., STU-25-12)
            // FIX: We do NOT append the grade here. All students share the same counter for the month.
            string sequenceKey = $"{rolePrefix}-{year}-{month}";

            // Call the Repository to get the number
            int nextNumber = await _sequenceRepository.GetNextSequenceNumberAsync(sequenceKey);

            // Format (e.g., 00001)
            string incrementPart = nextNumber.ToString("D5");

            // Result: STU + 2512 + 00001 => STU251200001
            return $"{rolePrefix}{middlePart}{incrementPart}";
        }

        // This helper is no longer needed for ID generation but kept just in case you use it elsewhere, 
        // or you can remove it if unused.
        private string ExtractClassNumber(string? grade)
        {
            if (string.IsNullOrEmpty(grade)) return "0";
            var match = Regex.Match(grade, @"\d+");
            if (match.Success)
            {
                return match.Value;
            }
            return "0";
        }
    }
}