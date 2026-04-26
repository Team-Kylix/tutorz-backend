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

        /// <summary>
        /// Gets disputes scoped to the caller:
        /// SuperAdmin sees all; regular Admin sees Pending + their own assigned disputes.
        /// </summary>
        Task<PaginatedResultDto<DisputeResponseDto>> GetAllDisputesAsync(
            string? searchQuery, int page, int pageSize,
            Guid callerAdminUserId, bool isSuperAdmin);

        Task<PaginatedResultDto<DisputeResponseDto>> GetDisputesByUserIdAsync(Guid userId, int page, int pageSize);
        Task<string> GenerateDisputeNumberAsync();

        /// <summary>
        /// Updates status and assigns the calling admin if not yet assigned.
        /// Returns false if the dispute is locked by a different admin (and caller is not SuperAdmin).
        /// </summary>
        Task<(bool Success, string? Error)> UpdateStatusAsync(
            int disputeId, UpdateDisputeStatusDto dto,
            Guid callerAdminUserId, bool isSuperAdmin);
    }
}
