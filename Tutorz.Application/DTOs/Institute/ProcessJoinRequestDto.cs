using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Institute
{
    public class ProcessJoinRequestDto
    {
        [Required]
        public string Action { get; set; } // "Accept" or "Decline"
    }
}
