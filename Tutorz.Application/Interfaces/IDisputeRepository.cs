using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Disputes;
using Tutorz.Application.DTOs.Common;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IDisputeRepository : IGenericRepository<Dispute>
    {
        Task<DisputeResponseDto?> GetDisputeByIdAsync(int disputeId);
        Task<PaginatedResultDto<DisputeResponseDto>> GetAllDisputesAsync(string? searchQuery, int page, int pageSize);
        Task<PaginatedResultDto<DisputeResponseDto>> GetDisputesByUserIdAsync(Guid userId, int page, int pageSize);
        Task<string> GenerateDisputeNumberAsync();
        Task<bool> UpdateStatusAsync(int disputeId, UpdateDisputeStatusDto dto);
    }
}
