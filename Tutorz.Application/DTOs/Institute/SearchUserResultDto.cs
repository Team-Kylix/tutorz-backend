using System;

namespace Tutorz.Application.DTOs.Institute
{
    public class SearchUserResultDto
    {
        public Guid UserId { get; set; }
        public Guid RoleSpecificId { get; set; } // StudentId or TutorId
        public string RegistrationNumber { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsAlreadyAssigned { get; set; } // indicates if they are already in this institute
    }
}
