using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepo;
        private readonly IGenericRepository<Class> _classRepo; 
        private readonly IStudentRepository _studentRepo;
        private readonly IUserRepository _userRepo;

        public TutorService(ITutorRepository tutorRepo, IGenericRepository<Class> classRepo, IStudentRepository studentRepo, IUserRepository userRepo)
        {
            _tutorRepo = tutorRepo;
            _classRepo = classRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
        }

        public async Task<ClassDto> CreateClassAsync(Guid userId, CreateClassRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) throw new Exception("Tutor profile not found.");

            // Fetch all active classes/seminars for this tutor
            var existingClasses = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && c.IsActive);

            // Convert new start/end to comparable integers (e.g., "14:30" -> 1430)
            int newStart = int.Parse(request.StartTime.Replace(":", ""));
            int newEnd = int.Parse(request.EndTime.Replace(":", ""));

            // Determine the "Day of Week" for the new request
            string checkDay = request.DayOfWeek;
            DateTime? checkDate = request.Date;

            // If it's a Seminar (Date based), find out which Day of Week it falls on
            if (request.ClassType != "Class" && request.Date.HasValue)
            {
                checkDay = request.Date.Value.DayOfWeek.ToString(); // e.g. "Friday"
            }

            foreach (var existing in existingClasses)
            {
                bool isDayConflict = false;

                // Case A: New Item is Weekly Class
                if (request.ClassType == "Class")
                {
                    // Conflicts if existing is also Weekly on same day
                    if (existing.ClassType == "Class" && existing.DayOfWeek == request.DayOfWeek)
                        isDayConflict = true;

                    // Conflicts if existing is Seminar on this specific day (Future check could be added here)
                }
                // Case B: New Item is Seminar (Specific Date)
                else
                {
                    // Conflicts if existing is Seminar on exact same Date
                    if (existing.ClassType != "Class" && existing.Date.HasValue && checkDate.HasValue && existing.Date.Value.Date == checkDate.Value.Date)
                        isDayConflict = true;

                    // Conflicts if existing is Weekly Class on that Day of Week
                    if (existing.ClassType == "Class" && existing.DayOfWeek == checkDay)
                        isDayConflict = true;
                }

                if (isDayConflict)
                {
                    int exStart = int.Parse(existing.StartTime.Replace(":", ""));
                    int exEnd = int.Parse(existing.EndTime.Replace(":", ""));

                    // Overlap Check: (StartA < EndB) and (EndA > StartB)
                    if (newStart < exEnd && newEnd > exStart)
                    {
                        throw new Exception($"Time Crash! This time overlaps with your '{existing.ClassType}': {existing.Subject} ({existing.StartTime} - {existing.EndTime}). Please choose another time.");
                    }
                }
            }

            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TutorId = tutor.TutorId,
                InstituteName = request.InstituteName,
                ClassType = request.ClassType,
                Subject = request.Subject,
                Grade = request.Grade,
                ClassName = !string.IsNullOrEmpty(request.ClassName) ? request.ClassName : $"{request.Subject} ({request.ClassType})",
                DayOfWeek = request.DayOfWeek,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                HallName = request.HallName,
                Fee = request.Fee,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            await _classRepo.AddAsync(newClass);
            await _classRepo.SaveChangesAsync();

            return MapToDto(newClass);
        }



        public async Task<ClassDto> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var existingClass = await _classRepo.GetAsync(c => c.ClassId == classId && c.TutorId == tutor.TutorId);

            if (existingClass == null) throw new Exception("Class not found or access denied.");

            existingClass.InstituteName = request.InstituteName; 
            existingClass.ClassType = request.ClassType;         
            existingClass.Subject = request.Subject;
            existingClass.Grade = request.Grade;
            existingClass.ClassName = request.ClassName;
            existingClass.DayOfWeek = request.DayOfWeek;
            existingClass.Date = request.Date;                  
            existingClass.StartTime = request.StartTime;
            existingClass.EndTime = request.EndTime;
            existingClass.HallName = request.HallName;
            existingClass.Fee = request.Fee;
            existingClass.IsActive = request.IsActive;
            existingClass.UpdatedDate = DateTime.UtcNow;

            await _classRepo.SaveChangesAsync();
            return MapToDto(existingClass);
        }

        public async Task DeleteClassAsync(Guid classId, Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var existingClass = await _classRepo.GetAsync(c => c.ClassId == classId && c.TutorId == tutor.TutorId);
            if (existingClass == null) throw new Exception("Class not found.");

            await _classRepo.DeleteAsync(existingClass);
        }

        public async Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var student = await _studentRepo.GetAsync(s => s.UserId.ToString() == request.StudentRegistrationNumber); // Simplified lookup
            if (student == null) throw new Exception("Student not found.");

            return true;
        }

        public async Task<List<ClassDto>> GetClassesAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) throw new Exception("Tutor profile not found.");

            var classes = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId);

            return classes.Select(c => new ClassDto
            {
                ClassId = c.ClassId,
                InstituteName = c.InstituteName, 
                ClassType = c.ClassType,         
                Subject = c.Subject,
                Grade = c.Grade,
                ClassName = c.ClassName,
                DayOfWeek = c.DayOfWeek,
                Date = c.Date,                   
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                HallName = c.HallName,
                Fee = c.Fee,
                IsActive = c.IsActive,
                StudentCount = 0
            }).ToList();
        }


        public async Task<ServiceResponse<TutorProfileDto>> UpdateTutorProfileAsync(Guid userId, TutorProfileDto request)
        {
            var response = new ServiceResponse<TutorProfileDto>();

            // 1. Get Tutor Entity
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null)
            {
                response.Success = false;
                response.Message = "Tutor not found.";
                return response;
            }

            // 2. Get User Entity (For Phone Number)
            var user = await _userRepo.GetAsync(u => u.UserId == userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                return response;
            }

            // 3. Update Fields
            tutor.FirstName = request.FirstName;
            tutor.LastName = request.LastName;
            tutor.Bio = request.Bio;
            tutor.BankName = request.BankName;
            tutor.BankAccountNumber = request.BankAccountNumber;
            tutor.UpdatedDate = DateTime.UtcNow;

            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedDate = DateTime.UtcNow;

            // 4. Save Changes
            await _tutorRepo.SaveChangesAsync();
            await _userRepo.SaveChangesAsync();

            // 5. Return updated profile
            // We call GetTutorProfileAsync internally to ensure we return the standard DTO
            var updatedProfile = await GetTutorProfileAsync(userId);
            response.Data = updatedProfile.Data;
            response.Success = true;
            response.Message = "Profile updated successfully";

            return response;
        }

        private ClassDto MapToDto(Class entity)
        {
            return new ClassDto
            {
                ClassId = entity.ClassId,
                InstituteName = entity.InstituteName,
                ClassType = entity.ClassType,
                Subject = entity.Subject,
                Grade = entity.Grade,
                ClassName = entity.ClassName,
                DayOfWeek = entity.DayOfWeek,
                Date = entity.Date,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                HallName = entity.HallName,
                Fee = entity.Fee,
                IsActive = entity.IsActive,
                StudentCount = 0
            };
        }
    

        public async Task<ServiceResponse<TutorProfileDto>> GetTutorProfileAsync(Guid userId)
        {
            var response = new ServiceResponse<TutorProfileDto>();

            // We use your existing Repository method here
            var profileDto = await _tutorRepo.GetTutorProfileAsync(userId);

            if (profileDto == null)
            {
                response.Success = false;
                response.Message = "Profile not found";
                return response;
            }

            response.Data = profileDto;
            return response;
        }
    }
}
