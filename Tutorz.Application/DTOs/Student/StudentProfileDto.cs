using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Tutorz.Application.DTOs.Student
{
    public class StudentProfileDto
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SchoolName { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string RegistrationNumber { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPrimary { get; set; }
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
    }

    public class UpdateStudentProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SchoolName { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }

}