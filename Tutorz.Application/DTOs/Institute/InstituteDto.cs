using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Institute
{
    public class InstituteDto
    {
        public Guid InstituteId { get; set; }
        public string Name { get; set; }
        public string? City { get; set; }
    }
}
