using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Services
{
    public class HallService : IHallService
    {
        private readonly IHallRepository _hallRepository;
        private readonly IInstituteRepository _instituteRepository;
        private readonly IGenericRepository<Class> _classRepository;

        public HallService(IHallRepository hallRepository, IInstituteRepository instituteRepository, IGenericRepository<Class> classRepository)
        {
            _hallRepository = hallRepository;
            _instituteRepository = instituteRepository;
            _classRepository = classRepository;
        }

        public async Task<ServiceResponse<HallDto>> AddHallAsync(Guid instituteId, CreateHallDto dto)
        {
            // Verify Institute exists (using UserId as InstituteId or linking via Institute table)
            // The controller passes the Guid. We assume it's the InstituteId (primary key of Institute table) or UserId.
            // Let's assume the controller resolves the correct InstituteId.
            // However, based on previous code, User.FindFirst("InstituteId") return InstituteId.
            
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId);
            string regNum = institute?.RegistrationNumber ?? "UNKNOWN";
            
            // Format: [RegistrationNumber]HALL[Name] -> e.g. INS26201HALL1
            string customHallCode = $"{regNum}HALL{dto.Name}";

            var hall = new Hall
            {
                HallId = Guid.NewGuid(),
                InstituteId = instituteId,
                HallCode = customHallCode,
                Name = dto.Name,
                Capacity = dto.Capacity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _hallRepository.AddAsync(hall);
            await _hallRepository.SaveChangesAsync();

            return new ServiceResponse<HallDto>
            {
                Success = true,
                Data = new HallDto
                {
                    HallId = hall.HallId,
                    HallCode = hall.HallCode,
                    Name = hall.Name,
                    Capacity = hall.Capacity,
                    IsActive = hall.IsActive
                },
                Message = "Hall added successfully."
            };
        }

        public async Task<ServiceResponse<IEnumerable<HallDto>>> GetHallsAsync(Guid instituteId)
        {
            var halls = await _hallRepository.GetAllAsync(h => h.InstituteId == instituteId && !h.IsDeleted);
            
            var dtos = halls.Select(h => new HallDto
            {
                HallId = h.HallId,
                HallCode = h.HallCode,
                Name = h.Name,
                Capacity = h.Capacity,
                IsActive = h.IsActive
            });

            return new ServiceResponse<IEnumerable<HallDto>>
            {
                Success = true,
                Data = dtos
            };
        }

        public async Task<ServiceResponse<HallDto>> UpdateHallAsync(Guid instituteId, Guid hallId, CreateHallDto dto)
        {
            var hall = await _hallRepository.GetAsync(h => h.HallId == hallId && h.InstituteId == instituteId && !h.IsDeleted);
            if (hall == null)
            {
                return new ServiceResponse<HallDto>
                {
                    Success = false,
                    Message = "Hall not found or does not belong to this institute."
                };
            }

            hall.Name = dto.Name;
            hall.Capacity = dto.Capacity;
            
            // If Name changes, should we update HallCode? 
            // The user requested Format: [RegistrationNumber]HALL[Name].
            // If Name changes, HallCode SHOULD ideally update to reflect it.
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId);
            string regNum = institute?.RegistrationNumber ?? "UNKNOWN";
            hall.HallCode = $"{regNum}HALL{dto.Name}";

            await _hallRepository.SaveChangesAsync();

            return new ServiceResponse<HallDto>
            {
                Success = true,
                Data = new HallDto
                {
                    HallId = hall.HallId,
                    HallCode = hall.HallCode,
                    Name = hall.Name,
                    Capacity = hall.Capacity,
                    IsActive = hall.IsActive
                },
                Message = "Hall updated successfully."
            };
        }

        public async Task<ServiceResponse<bool>> DeleteHallAsync(Guid instituteId, Guid hallId)
        {
            var hall = await _hallRepository.GetAsync(h => h.HallId == hallId && h.InstituteId == instituteId && !h.IsDeleted);
            if (hall == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Hall not found or does not belong to this institute."
                };
            }

            var activeClasses = await _classRepository.GetAllAsync(c => c.InstituteId == instituteId && c.HallName == hall.Name && !c.IsDeleted && c.IsActive);
            
            if (activeClasses.Any())
            {
                var classNames = string.Join(", ", activeClasses.Select(c => c.ClassName ?? c.Subject));
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"These classes are already held on this hall: {classNames}. Change these classes into another hall to delete this hall."
                };
            }

            // Soft Delete
            hall.IsDeleted = true;
            // await _hallRepository.DeleteAsync(hall); // Removed actual delete
            await _hallRepository.SaveChangesAsync();

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Hall deleted successfully."
            };
        }

        public async Task<ServiceResponse<HallDto>> ToggleHallStatusAsync(Guid instituteId, Guid hallId)
        {
            var hall = await _hallRepository.GetAsync(h => h.HallId == hallId && h.InstituteId == instituteId && !h.IsDeleted);
            if (hall == null)
            {
                return new ServiceResponse<HallDto>
                {
                    Success = false,
                    Message = "Hall not found or does not belong to this institute."
                };
            }

            hall.IsActive = !hall.IsActive;
            await _hallRepository.SaveChangesAsync();

            return new ServiceResponse<HallDto>
            {
                Success = true,
                Data = new HallDto
                {
                    HallId = hall.HallId,
                    HallCode = hall.HallCode,
                    Name = hall.Name,
                    Capacity = hall.Capacity,
                    IsActive = hall.IsActive
                },
                Message = $"Hall {(hall.IsActive ? "activated" : "deactivated")} successfully."
            };
        }

    }
}
