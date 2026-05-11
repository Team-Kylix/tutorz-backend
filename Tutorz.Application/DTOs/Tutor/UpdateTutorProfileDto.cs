using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Tutor
{
    public class UpdateTutorProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Bio { get; set; }
        public string? Address { get; set; }
        public int? CityId { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? ProfilePicture { get; set; }
    }
}
