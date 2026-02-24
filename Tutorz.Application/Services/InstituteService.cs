using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Application.Services
{
    public class InstituteService : IInstituteService
    {

        private readonly IInstituteRepository _instituteRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInstituteStudentRepository _instituteStudentRepository;
        private readonly IInstituteTutorRepository _instituteTutorRepository;

        public InstituteService(
            IInstituteRepository instituteRepository,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IUserRepository userRepository,
            IInstituteStudentRepository instituteStudentRepository,
            IInstituteTutorRepository instituteTutorRepository)
        {
            _instituteRepository = instituteRepository;
            _studentRepository = studentRepository;
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
            _instituteStudentRepository = instituteStudentRepository;
            _instituteTutorRepository = instituteTutorRepository;
        }

        public async Task<ServiceResponse<InstituteProfileDto>> GetProfileAsync(Guid instituteId)
        {
            
            var institute = await _instituteRepository.GetAsync(
                expression: i => i.InstituteId == instituteId || i.UserId == instituteId,
                includeProperties: "User"
            );

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

            var dto = new InstituteProfileDto
            {
                InstituteId = institute.InstituteId,
                RegistrationNumber = institute.RegistrationNumber,
                InstituteName = institute.InstituteName,
                Address = institute.Address,
                ContactNumber = institute.ContactNumber,
                Website = institute.Website,
                Email = (institute.User != null) ? institute.User.Email : "",
                CityId = (institute.User != null) ? institute.User.CityId : null
            };

            return new ServiceResponse<InstituteProfileDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<InstituteProfileDto>> UpdateProfileAsync(Guid id, UpdateInstituteProfileDto dto)
        {
            
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == id || i.UserId == id);

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

       
            institute.InstituteName = dto.InstituteName;
            institute.Address = dto.Address;
            institute.ContactNumber = dto.ContactNumber;
            institute.Website = dto.Website;
            institute.UpdatedDate = DateTime.UtcNow;

            await _instituteRepository.SaveChangesAsync();

            
            return await GetProfileAsync(institute.InstituteId);
        }

        public async Task<ServiceResponse<bool>> AssignStudentAsync(Guid instituteId, AssignStudentDto dto)
        {
            var exists = await _instituteStudentRepository.GetAsync(is_ => is_.InstituteId == instituteId && is_.StudentId == dto.StudentId);
            if (exists != null)
                return new ServiceResponse<bool> { Success = false, Message = "Student is already assigned to this institute." };

            await _instituteStudentRepository.AddAsync(new InstituteStudent
            {
                InstituteId = instituteId,
                StudentId = dto.StudentId,
                AssignedDate = DateTime.UtcNow
            });
            await _instituteStudentRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Student assigned successfully." };
        }

        public async Task<ServiceResponse<bool>> AssignTutorAsync(Guid instituteId, AssignTutorDto dto)
        {
            var exists = await _instituteTutorRepository.GetAsync(it => it.InstituteId == instituteId && it.TutorId == dto.TutorId);
            if (exists != null)
                return new ServiceResponse<bool> { Success = false, Message = "Tutor is already assigned to this institute." };

            await _instituteTutorRepository.AddAsync(new InstituteTutor
            {
                InstituteId = instituteId,
                TutorId = dto.TutorId,
                AssignedDate = DateTime.UtcNow
            });
            await _instituteTutorRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Tutor assigned successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower();

            // Just a basic search mechanism fetching all and filtering in memory or using where
            // For production, this should preferably use IQueryable or full text search
            var allStudents = await _studentRepository.GetAllAsync();
            
            var matchedStudents = allStudents.Where(s => 
                s.FirstName.ToLower().Contains(query) || 
                s.LastName.ToLower().Contains(query) ||
                s.RegistrationNumber.ToLower().Contains(query)
            ).ToList();

            var assignedStudents = await _instituteStudentRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedStudentIds = assignedStudents.Select(a => a.StudentId).ToHashSet();

            var results = new List<SearchUserResultDto>();
            foreach (var student in matchedStudents)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == student.UserId);
                results.Add(new SearchUserResultDto
                {
                    UserId = student.UserId,
                    RoleSpecificId = student.StudentId,
                    RegistrationNumber = student.RegistrationNumber,
                    Name = $"{student.FirstName} {student.LastName}",
                    PhoneNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    IsAlreadyAssigned = assignedStudentIds.Contains(student.StudentId)
                });
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower();

            var allTutors = await _tutorRepository.GetAllAsync();
            var matchedTutors = allTutors.Where(t => 
                t.FirstName.ToLower().Contains(query) || 
                t.LastName.ToLower().Contains(query) ||
                t.RegistrationNumber.ToLower().Contains(query)
            ).ToList();

            var assignedTutors = await _instituteTutorRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedTutorIds = assignedTutors.Select(a => a.TutorId).ToHashSet();

            var results = new List<SearchUserResultDto>();
            foreach (var tutor in matchedTutors)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == tutor.UserId);
                results.Add(new SearchUserResultDto
                {
                    UserId = tutor.UserId,
                    RoleSpecificId = tutor.TutorId,
                    RegistrationNumber = tutor.RegistrationNumber,
                    Name = $"{tutor.FirstName} {tutor.LastName}",
                    PhoneNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    IsAlreadyAssigned = assignedTutorIds.Contains(tutor.TutorId)
                });
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
        }

        public async Task<ServiceResponse<PaginatedResultDto<StudentProfileDto>>> GetAssignedStudentsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10)
        {
            var assigned = await _instituteStudentRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var studentIds = assigned.Select(a => a.StudentId).ToList();

            var query = await _studentRepository.GetAllAsync();
            var instituteStudents = query.Where(s => studentIds.Contains(s.StudentId));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                instituteStudents = instituteStudents.Where(s => 
                    s.FirstName.ToLower().Contains(lowerQuery) || 
                    s.LastName.ToLower().Contains(lowerQuery) ||
                    s.RegistrationNumber.ToLower().Contains(lowerQuery)
                );
            }

            var totalCount = instituteStudents.Count();
            var pagedStudents = instituteStudents
                .OrderBy(s => s.FirstName) // Predictable ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var profiles = new List<StudentProfileDto>();
            foreach (var student in pagedStudents)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == student.UserId);
                profiles.Add(new StudentProfileDto
                {
                    StudentId = student.StudentId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Grade = student.Grade,
                    IsPrimary = student.IsPrimary,
                    RegistrationNumber = student.RegistrationNumber,
                    Email = user?.Email,
                    PhoneNumber = user?.PhoneNumber
                });
            }

            var result = new PaginatedResultDto<StudentProfileDto>
            {
                Items = profiles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new ServiceResponse<PaginatedResultDto<StudentProfileDto>> { Success = true, Data = result };
        }

        public async Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAssignedTutorsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10)
        {
            var assigned = await _instituteTutorRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var tutorIds = assigned.Select(a => a.TutorId).ToList();

            var query = await _tutorRepository.GetAllAsync();
            var instituteTutors = query.Where(t => tutorIds.Contains(t.TutorId));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                instituteTutors = instituteTutors.Where(t => 
                    t.FirstName.ToLower().Contains(lowerQuery) || 
                    t.LastName.ToLower().Contains(lowerQuery) ||
                    t.RegistrationNumber.ToLower().Contains(lowerQuery)
                );
            }

            var totalCount = instituteTutors.Count();
            var pagedTutors = instituteTutors
                .OrderBy(t => t.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var profiles = new List<TutorProfileDto>();
            foreach (var tutor in pagedTutors)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == tutor.UserId);
                profiles.Add(new TutorProfileDto
                {
                    TutorId = tutor.TutorId,
                    FirstName = tutor.FirstName,
                    LastName = tutor.LastName,
                    Bio = tutor.Bio,
                    ExperienceYears = tutor.ExperienceYears,
                    Email = user?.Email,
                    PhoneNumber = user?.PhoneNumber,
                    ProfileImageUrl = user?.QrCodeUrl, // Or actual profile image
                    RegistrationNumber = tutor.RegistrationNumber
                });
            }

            var result = new PaginatedResultDto<TutorProfileDto>
            {
                Items = profiles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new ServiceResponse<PaginatedResultDto<TutorProfileDto>> { Success = true, Data = result };
        }
    }
}