using Tutorz.Domain.Enums;

namespace Tutorz.Application.DTOs.Disputes
{
    public class UpdateDisputeStatusDto
    {
        public DisputeStatus Status { get; set; }
        public string? AdminNote { get; set; }
    }
}
