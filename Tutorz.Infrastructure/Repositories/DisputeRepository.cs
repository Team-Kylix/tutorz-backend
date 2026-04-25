using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Disputes;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Domain.Enums;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class DisputeRepository : GenericRepository<Dispute>, IDisputeRepository
    {
        public DisputeRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<string> GenerateDisputeNumberAsync()
        {
            int nextId = await _context.Disputes.AnyAsync()
                ? (await _context.Disputes.MaxAsync(d => d.Id)) + 1
                : 1;
            return $"CMP-{nextId:D5}";
        }

        public async Task<DisputeResponseDto?> GetDisputeByIdAsync(int disputeId)
        {
            var d = await _context.Disputes
                .Include(x => x.RaisedByUser)
                .FirstOrDefaultAsync(x => x.Id == disputeId);

            if (d == null) return null;

            var roleName = await _context.Roles
                .Where(r => r.RoleId == d.RaisedByUser.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            return BuildDto(d, roleName);
        }

        public async Task<PaginatedResultDto<DisputeResponseDto>> GetAllDisputesAsync(string? searchQuery, int page, int pageSize)
        {
            var query = _context.Disputes
                .Include(d => d.RaisedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.ToLower();
                query = query.Where(d =>
                    d.DisputeNumber.ToLower().Contains(term) ||
                    d.Title.ToLower().Contains(term) ||
                    (d.RaisedByUser != null && d.RaisedByUser.PhoneNumber != null && d.RaisedByUser.PhoneNumber.Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var disputes = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Resolve role names in one query
            var roleIds   = disputes.Select(d => d.RaisedByUser?.RoleId).Distinct().ToList();
            var roleNames = await _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .ToDictionaryAsync(r => r.RoleId, r => r.Name);

            var items = disputes.Select(d =>
            {
                var roleName = d.RaisedByUser != null && roleNames.ContainsKey(d.RaisedByUser.RoleId)
                    ? roleNames[d.RaisedByUser.RoleId]
                    : "Unknown";
                return BuildDto(d, roleName);
            }).ToList();

            return new PaginatedResultDto<DisputeResponseDto>
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<PaginatedResultDto<DisputeResponseDto>> GetDisputesByUserIdAsync(Guid userId, int page, int pageSize)
        {
            var query = _context.Disputes
                .Include(d => d.RaisedByUser)
                .Where(d => d.RaisedByUserId == userId)
                .OrderByDescending(d => d.CreatedAt);

            var totalCount = await query.CountAsync();

            var disputes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // User's role won't change between records — fetch once
            var user = disputes.FirstOrDefault()?.RaisedByUser;
            var roleName = "Unknown";
            if (user != null)
            {
                roleName = await _context.Roles
                    .Where(r => r.RoleId == user.RoleId)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync() ?? "Unknown";
            }

            var items = disputes.Select(d => BuildDto(d, roleName)).ToList();

            return new PaginatedResultDto<DisputeResponseDto>
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<bool> UpdateStatusAsync(int disputeId, UpdateDisputeStatusDto dto)
        {
            var dispute = await _context.Disputes.FindAsync(disputeId);
            if (dispute == null) return false;

            dispute.Status    = dto.Status;
            dispute.AdminNote = dto.AdminNote;
            dispute.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Private helpers ─────────────────────────────────────────────────
        private static DisputeResponseDto BuildDto(Dispute d, string roleName) => new DisputeResponseDto
        {
            Id             = d.Id,
            DisputeNumber  = d.DisputeNumber,
            Title          = d.Title,
            Description    = d.Description,
            ScreenshotUrl  = d.ScreenshotUrl,
            Category       = d.Category,
            CategoryLabel  = GetCategoryLabel(d.Category),
            Status         = d.Status,
            StatusLabel    = GetStatusLabel(d.Status),
            AdminNote      = d.AdminNote,
            RaisedByUserId = d.RaisedByUserId,
            RaisedByName   = d.RaisedByUser != null ? d.RaisedByUser.Email ?? d.RaisedByUser.PhoneNumber ?? "Unknown" : "Unknown",
            RaisedByRole   = roleName,
            RaisedByPhone  = d.RaisedByUser?.PhoneNumber ?? "",
            CreatedAt      = d.CreatedAt,
            UpdatedAt      = d.UpdatedAt
        };

        private static string GetStatusLabel(DisputeStatus status) => status switch
        {
            DisputeStatus.Pending     => "Pending",
            DisputeStatus.UnderReview => "Under Review",
            DisputeStatus.Resolved    => "Resolved",
            DisputeStatus.Rejected    => "Rejected",
            DisputeStatus.Closed      => "Closed",
            _                         => "Unknown"
        };

        private static string GetCategoryLabel(DisputeCategory category) => category switch
        {
            DisputeCategory.Financial       => "Financial",
            DisputeCategory.DataError       => "Data Error",
            DisputeCategory.TechnicalIssue  => "Technical Issue",
            DisputeCategory.AccountAccess   => "Account Access",
            DisputeCategory.ClassEnrollment => "Class Enrollment",
            DisputeCategory.AttendanceIssue => "Attendance Issue",
            DisputeCategory.PaymentIssue    => "Payment Issue",
            DisputeCategory.Other           => "Other",
            _                               => "Other"
        };
    }
}
