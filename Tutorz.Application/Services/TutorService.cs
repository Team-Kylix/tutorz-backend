using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepo;
        private readonly IGenericRepository<Class> _classRepo; // Ensure you register this in Program.cs
        private readonly IStudentRepository _studentRepo;
        // Inject DbContext if you need complex joins or use a specific ClassRepository

        public TutorService(ITutorRepository tutorRepo, IGenericRepository<Class> classRepo, IStudentRepository studentRepo)
        {
            _tutorRepo = tutorRepo;
            _classRepo = classRepo;
            _studentRepo = studentRepo;
        }

        public async Task<ClassDto> CreateClassAsync(Guid userId, CreateClassRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) throw new Exception("Tutor profile not found.");

            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TutorId = tutor.TutorId,
                Subject = request.Subject,
                Grade = request.Grade,
                ClassName = request.ClassName ?? $"{request.Subject} - {request.Grade}",
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                HallName = request.HallName,
                Fee = request.Fee
            };

            await _classRepo.AddAsync(newClass);
            await _classRepo.SaveChangesAsync();

            return MapToDto(newClass);
        }

        public async Task<ClassDto> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request)
        {
            // Verify ownership
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var existingClass = await _classRepo.GetAsync(c => c.ClassId == classId && c.TutorId == tutor.TutorId);

            if (existingClass == null) throw new Exception("Class not found or access denied.");

            existingClass.Subject = request.Subject;
            existingClass.Grade = request.Grade;
            existingClass.ClassName = request.ClassName;
            existingClass.DayOfWeek = request.DayOfWeek;
            existingClass.StartTime = request.StartTime;
            existingClass.EndTime = request.EndTime;
            existingClass.HallName = request.HallName;
            existingClass.Fee = request.Fee;

            await _classRepo.SaveChangesAsync();
            return MapToDto(existingClass);
        }

        public async Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            // In a real app, use Include() to load Students. For GenericRepo, you might need a specific method.
            // This logic assumes you have access to a context or a specific method in Repository.
            // Placeholder logic:
            var student = await _studentRepo.GetAsync(s => s.UserId.ToString() == request.StudentRegistrationNumber); // Simplified lookup
            if (student == null) throw new Exception("Student not found.");

            // Add logic to save to ClassStudent table
            return true;
        }

        public async Task<List<ClassDto>> GetClassesAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            // Assuming generic repo can return list or you add GetListAsync
            // This is pseudo-code for the query: context.Classes.Where(c => c.TutorId == tutor.TutorId).ToList();
            return new List<ClassDto>(); // Implement actual fetch
        }

        private ClassDto MapToDto(Class entity)
        {
            return new ClassDto
            {
                ClassId = entity.ClassId,
                Subject = entity.Subject,
                Grade = entity.Grade,
                ClassName = entity.ClassName,
                DayOfWeek = entity.DayOfWeek,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                HallName = entity.HallName,
                Fee = entity.Fee,
                StudentCount = 0 // Calculate this
            };
        }
    }
}
