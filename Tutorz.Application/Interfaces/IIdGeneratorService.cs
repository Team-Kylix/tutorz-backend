using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IIdGeneratorService
    {
        /// <summary>
        /// Generates a unique registration ID based on role and optional class/grade info.
        /// </summary>
        /// <param name="roleName">Student, Tutor, Institute, or Admin</param>
        /// <param name="gradeOrClass">Only required for Students (e.g., "Grade 10")</param>
        /// <returns>Formatted ID (e.g., STU256800084)</returns>
        Task<string> GenerateNextIdAsync(string roleName, string? gradeOrClass = null);
    }
}
