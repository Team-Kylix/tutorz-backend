using AutoMapper;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Mappings
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Class, ClassDto>();
            CreateMap<CreateClassRequest, Class>();
            // Add other mappings here as needed
        }
    }
}
