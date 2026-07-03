using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Tutor
{
    public class ProcessRequestDto
    {
        public List<Guid> EnrollmentIds { get; set; }
        public string Action { get; set; } 
    }
}
