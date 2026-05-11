using Microsoft.AspNetCore.Http;
using Tutorz.Domain.Enums;

namespace Tutorz.Application.DTOs.Disputes
{
    public class CreateDisputeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DisputeCategory Category { get; set; } = DisputeCategory.Other;
        public IFormFile? Screenshot { get; set; }
    }
}
