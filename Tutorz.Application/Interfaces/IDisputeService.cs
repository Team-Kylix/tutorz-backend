using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Disputes;

namespace Tutorz.Application.Interfaces
{
    public interface IDisputeService
    {
        /// <summary>Raise a new complaint. Handles screenshot upload to Azure Blob.</summary>
        Task<ServiceResponse<DisputeResponseDto>> CreateDisputeAsync(Guid userId, CreateDisputeDto dto);

        /// <summary>Get a single dispute by ID (caller must own it, or be Admin).</summary>
        Task<ServiceResponse<DisputeResponseDto>> GetDisputeByIdAsync(int disputeId, Guid callerUserId, bool isAdmin);

        /// <summary>Get paginated disputes raised by the current user.</summary>
        Task<ServiceResponse<PaginatedResultDto<DisputeResponseDto>>> GetMyDisputesAsync(Guid userId, int page, int pageSize);

        /// <summary>
        /// Admin: get disputes scoped to the caller.
        /// SuperAdmin sees all; regular Admin sees Pending + their own assigned.
        /// </summary>
        Task<ServiceResponse<PaginatedResultDto<DisputeResponseDto>>> GetAllDisputesAsync(
            string? searchQuery, int page, int pageSize,
            Guid callerAdminUserId, bool isSuperAdmin);

        /// <summary>
        /// Admin: update the status and optionally add a note.
        /// Automatically assigns the dispute to the calling admin on first status change.
        /// Returns failure if the dispute is locked by a different admin.
        /// </summary>
        Task<ServiceResponse<bool>> UpdateDisputeStatusAsync(
            int disputeId, UpdateDisputeStatusDto dto,
            Guid callerAdminUserId, bool isSuperAdmin);

        /// <summary>
        /// Soft deletes a dispute if it is in Pending status and belongs to the caller.
        /// </summary>
        Task<ServiceResponse<bool>> DeleteDisputeAsync(int disputeId, Guid callerUserId);
    }
}
