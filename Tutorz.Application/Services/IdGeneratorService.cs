using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;

namespace Tutorz.Application.Services
{
    public class IdGeneratorService : IIdGeneratorService
    {
        // Use the Interface, not the DbContext
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
            string middlePart = "";

            switch (roleName.ToLower())
            {
                case "student":
                    rolePrefix = "STU";
                    string classNum = ExtractClassNumber(gradeOrClass);
                    middlePart = $"{year}{month}{classNum}";
                    break;
                case "tutor":
                    rolePrefix = "TUT";
                    middlePart = $"{year}{month}";
                    break;
                case "institute":
                    rolePrefix = "INS";
                    middlePart = $"{year}{month}";
                    break;
                case "admin":
                    rolePrefix = "ADM";
                    middlePart = $"{year}{month}";
                    break;
                default:
                    throw new Exception("Unknown Role for ID generation");
            }

            // Generate Key
            string sequenceKey = $"{rolePrefix}-{year}-{month}";
            if (roleName.ToLower() == "student") sequenceKey += $"-{ExtractClassNumber(gradeOrClass)}";

            // Call the Repository to get the number (No DB logic here)
            int nextNumber = await _sequenceRepository.GetNextSequenceNumberAsync(sequenceKey);

            // Format
            string incrementPart = nextNumber.ToString("D5");

            return $"{rolePrefix}{middlePart}{incrementPart}";
        }

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