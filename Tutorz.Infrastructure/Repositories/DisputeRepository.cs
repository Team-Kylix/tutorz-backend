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
                .Include(x => x.AssignedAdmin)
                .FirstOrDefaultAsync(x => x.Id == disputeId);

            if (d == null) return null;

            var roleName = await _context.Roles
                .Where(r => r.RoleId == d.RaisedByUser.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            // Resolve actual name of the person who raised it
            string raisedByName = await ResolveUserFullNameAsync(d.RaisedByUserId, roleName);

            // Resolve assigned admin full name from the Admins table
            string? assignedAdminName = null;
            if (d.AssignedAdminUserId.HasValue)
            {
                var adminEntity = await _context.Admins
                    .FirstOrDefaultAsync(a => a.UserId == d.AssignedAdminUserId.Value);
                if (adminEntity != null)
                    assignedAdminName = $"{adminEntity.FirstName} {adminEntity.LastName}".Trim();
            }

            return BuildDto(d, roleName, raisedByName, assignedAdminName);
        }

        // ─── Scoped admin query ───────────────────────────────────────────────
        public async Task<PaginatedResultDto<DisputeResponseDto>> GetAllDisputesAsync(
            string? searchQuery, int page, int pageSize,
            Guid callerAdminUserId, bool isSuperAdmin)
        {
            var query = _context.Disputes
                .Include(d => d.RaisedByUser)
                .Include(d => d.AssignedAdmin)
                .AsQueryable();

            // ── Visibility scoping ──────────────────────────────────────────
            if (!isSuperAdmin)
            {
                // Regular admin: sees Pending (unassigned) OR disputes assigned to them
                query = query.Where(d =>
                    d.Status == DisputeStatus.Pending ||
                    d.AssignedAdminUserId == callerAdminUserId);
            }
            // SuperAdmin: no filter — sees everything

            // ── Search ──────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.ToLower();
                query = query.Where(d =>
                    d.DisputeNumber.ToLower().Contains(term) ||
                    d.Title.ToLower().Contains(term) ||
                    (d.RaisedByUser != null && d.RaisedByUser.PhoneNumber != null &&
                     d.RaisedByUser.PhoneNumber.Contains(term))
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

            // Resolve raised by user names in batches
            var raisedByUserIds = disputes.Select(d => d.RaisedByUserId).Distinct().ToList();
            
            var tutorNames = await _context.Tutors
                .Where(t => raisedByUserIds.Contains(t.UserId))
                .GroupBy(t => t.UserId)
                .Select(g => g.First())
                .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}".Trim());
            
            var studentNames = await _context.Students
                .Where(s => raisedByUserIds.Contains(s.UserId))
                .GroupBy(s => s.UserId)
                .Select(g => g.First())
                .ToDictionaryAsync(s => s.UserId, s => $"{s.FirstName} {s.LastName}".Trim());
            
            var instituteNames = await _context.Institutes
                .Where(i => raisedByUserIds.Contains(i.UserId))
                .GroupBy(i => i.UserId)
                .Select(g => g.First())
                .ToDictionaryAsync(i => i.UserId, i => i.InstituteName.Trim());
            
            var adminProfileNames = await _context.Admins
                .Where(a => raisedByUserIds.Contains(a.UserId))
                .GroupBy(a => a.UserId)
                .Select(g => g.First())
                .ToDictionaryAsync(a => a.UserId, a => $"{a.FirstName} {a.LastName}".Trim());

            // Resolve all assigned admin names in one query
            var assignedAdminUserIds = disputes
                .Where(d => d.AssignedAdminUserId.HasValue)
                .Select(d => d.AssignedAdminUserId!.Value)
                .Distinct()
                .ToList();

            var assignedAdminNames = await _context.Admins
                .Where(a => assignedAdminUserIds.Contains(a.UserId))
                .GroupBy(a => a.UserId)
                .Select(g => g.First())
                .ToDictionaryAsync(a => a.UserId, a => $"{a.FirstName} {a.LastName}".Trim());

            var items = disputes.Select(d =>
            {
                var roleName = d.RaisedByUser != null && roleNames.ContainsKey(d.RaisedByUser.RoleId)
                    ? roleNames[d.RaisedByUser.RoleId]
                    : "Unknown";

                string? adminName = null;
                if (d.AssignedAdminUserId.HasValue &&
                    assignedAdminNames.TryGetValue(d.AssignedAdminUserId.Value, out var n))
                    adminName = n;

                // Determine raised by name from pre-fetched dictionaries
                string raisedByName = "Unknown";
                if (tutorNames.TryGetValue(d.RaisedByUserId, out var tn)) raisedByName = tn;
                else if (studentNames.TryGetValue(d.RaisedByUserId, out var sn)) raisedByName = sn;
                else if (instituteNames.TryGetValue(d.RaisedByUserId, out var @in)) raisedByName = @in;
                else if (adminProfileNames.TryGetValue(d.RaisedByUserId, out var an)) raisedByName = an;
                else raisedByName = d.RaisedByUser?.Email ?? d.RaisedByUser?.PhoneNumber ?? "Unknown";

                return BuildDto(d, roleName, raisedByName, adminName);
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
            string raisedByName = "Unknown";
            if (user != null)
            {
                roleName = await _context.Roles
                    .Where(r => r.RoleId == user.RoleId)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync() ?? "Unknown";
                
                raisedByName = await ResolveUserFullNameAsync(userId, roleName);
            }

            var items = disputes.Select(d => BuildDto(d, roleName, raisedByName, null)).ToList();

            return new PaginatedResultDto<DisputeResponseDto>
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        // ─── Helper to resolve full name ──────────────────────────────────────
        private async Task<string> ResolveUserFullNameAsync(Guid userId, string roleName)
        {
            if (roleName == "Tutor")
            {
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tutor != null) return $"{tutor.FirstName} {tutor.LastName}".Trim();
            }
            else if (roleName == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null) return $"{student.FirstName} {student.LastName}".Trim();
            }
            else if (roleName == "Institute")
            {
                var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == userId);
                if (institute != null) return institute.InstituteName.Trim();
            }
            else if (roleName == "Admin" || roleName == "SuperAdmin")
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
                if (admin != null) return $"{admin.FirstName} {admin.LastName}".Trim();
            }

            var user = await _context.Users.FindAsync(userId);
            return user?.Email ?? user?.PhoneNumber ?? "Unknown";
        }

        // ─── Assignment + status update ───────────────────────────────────────
        public async Task<(bool Success, string? Error)> UpdateStatusAsync(
            int disputeId, UpdateDisputeStatusDto dto,
            Guid callerAdminUserId, bool isSuperAdmin)
        {
            var dispute = await _context.Disputes.FindAsync(disputeId);
            if (dispute == null) return (false, "Dispute not found.");

            // If already assigned to a different admin, only SuperAdmin may override
            if (dispute.AssignedAdminUserId.HasValue &&
                dispute.AssignedAdminUserId.Value != callerAdminUserId &&
                !isSuperAdmin)
            {
                return (false, "This dispute is already being handled by another admin.");
            }

            // Auto-assign on first status change away from Pending
            if (!dispute.AssignedAdminUserId.HasValue)
            {
                dispute.AssignedAdminUserId = callerAdminUserId;
            }

            dispute.Status    = dto.Status;
            dispute.AdminNote = dto.AdminNote;
            dispute.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ─── Private helpers ─────────────────────────────────────────────────
        private static DisputeResponseDto BuildDto(Dispute d, string roleName, string raisedByName, string? assignedAdminName) =>
            new DisputeResponseDto
            {
                Id                  = d.Id,
                DisputeNumber       = d.DisputeNumber,
                Title               = d.Title,
                Description         = d.Description,
                ScreenshotUrl       = d.ScreenshotUrl,
                Category            = d.Category,
                CategoryLabel       = GetCategoryLabel(d.Category),
                Status              = d.Status,
                StatusLabel         = GetStatusLabel(d.Status),
                AdminNote           = d.AdminNote,
                RaisedByUserId      = d.RaisedByUserId,
                RaisedByName        = raisedByName,
                RaisedByRole        = roleName,
                RaisedByPhone       = d.RaisedByUser?.PhoneNumber ?? "",
                AssignedAdminUserId = d.AssignedAdminUserId,
                AssignedAdminName   = assignedAdminName,
                CreatedAt           = d.CreatedAt,
                UpdatedAt           = d.UpdatedAt
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
