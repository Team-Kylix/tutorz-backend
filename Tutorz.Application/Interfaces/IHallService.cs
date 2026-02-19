using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;

namespace Tutorz.Application.Interfaces
{
    public interface IHallService
    {
        Task<ServiceResponse<HallDto>> AddHallAsync(Guid instituteId, CreateHallDto dto);
        Task<ServiceResponse<IEnumerable<HallDto>>> GetHallsAsync(Guid instituteId);
        Task<ServiceResponse<HallDto>> UpdateHallAsync(Guid instituteId, Guid hallId, CreateHallDto dto);
        Task<ServiceResponse<bool>> DeleteHallAsync(Guid instituteId, Guid hallId);
        Task<ServiceResponse<HallDto>> ToggleHallStatusAsync(Guid instituteId, Guid hallId);
    }
}
