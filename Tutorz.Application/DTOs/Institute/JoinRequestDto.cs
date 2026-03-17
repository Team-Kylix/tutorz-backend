using System;

namespace Tutorz.Application.DTOs.Institute
{
    public class JoinRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid InstituteId { get; set; }
        public string InstituteName { get; set; }
        
        // Target or Initiator details (depending on perspective)
        public Guid? TutorId { get; set; }
        public string TutorName { get; set; }
        
        public Guid? StudentId { get; set; }
        public string StudentName { get; set; }
        
        public string Status { get; set; } // "Pending", "Active", "Declined"
        public string InitiatedBy { get; set; } // "Institute" or "User"
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
