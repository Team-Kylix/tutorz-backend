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

        /// <summary>Admin: get all disputes with optional search.</summary>
        Task<ServiceResponse<PaginatedResultDto<DisputeResponseDto>>> GetAllDisputesAsync(string? searchQuery, int page, int pageSize);

        /// <summary>Admin: update the status and optionally add a note.</summary>
        Task<ServiceResponse<bool>> UpdateDisputeStatusAsync(int disputeId, UpdateDisputeStatusDto dto);
    }
}
