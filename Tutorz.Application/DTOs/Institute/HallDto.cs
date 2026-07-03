using System;

namespace Tutorz.Application.DTOs.Institute
{
    public class HallDto
    {
        public Guid HallId { get; set; }
        public string HallCode { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateHallDto
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
    }
}
