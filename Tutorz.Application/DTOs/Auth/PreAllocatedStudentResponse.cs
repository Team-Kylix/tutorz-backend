using System;

namespace Tutorz.Application.DTOs.Auth
{
    public class PreAllocatedStudentResponse
    {
        public string PreAllocatedRegNo { get; set; } = string.Empty;
        public Guid PreAllocatedUserId { get; set; }
        public Guid PreAllocatedStudentId { get; set; }
    }
}
