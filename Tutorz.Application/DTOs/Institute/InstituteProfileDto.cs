using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Tutorz.Application.DTOs.Institute
{
    public class InstituteProfileDto
    {
        public Guid InstituteId { get; set; }
        public string RegistrationNumber { get; set; }
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string? Website { get; set; }
        public string Email { get; set; } // Fetched from User table
        public int? CityId { get; set; }
        public bool IsSmsEnabled { get; set; }
        public decimal CommissionPercentage { get; set; }
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
    }

    public class UpdateInstituteProfileDto
    {
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string? Website { get; set; }
        public bool IsSmsEnabled { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}