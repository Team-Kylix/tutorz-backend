using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using Tutorz.Domain.Entities;
using Tutorz.Api.Hubs;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;

namespace Tutorz.Api.Controllers
{
    /// <summary>
    /// Controller for system-level information and health checks.
    /// Does not require authentication.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TutorzDbContext _dbContext;
        private readonly INotificationPusher _notificationPusher;
        private readonly IStudentService _studentService;
        private readonly ITutorService _tutorService;
        private readonly IInstituteService _instituteService;
        private readonly IAuthService _authService;
        private readonly IAdminService _adminService;
        private readonly IBillService _billService;
        private readonly IPaymentService _paymentService;

        public SystemController(
            IConfiguration configuration,
            TutorzDbContext dbContext,
            INotificationPusher notificationPusher,
            IStudentService studentService,
            ITutorService tutorService,
            IInstituteService instituteService,
            IAuthService authService,
            IAdminService adminService,
            IBillService billService,
            IPaymentService paymentService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _notificationPusher = notificationPusher;
            _studentService = studentService;
            _tutorService = tutorService;
            _instituteService = instituteService;
            _authService = authService;
            _adminService = adminService;
            _billService = billService;
            _paymentService = paymentService;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = _configuration["AppVersion"] ?? "0.0.0-unknown";
            return Ok(new { version });
        }

        [HttpGet("min-token-date")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetMinTokenDate()
        {
            var setting = await _dbContext.AppSettings.FindAsync("MinTokenDate");
            return Ok(new { minTokenDate = setting?.Value ?? "Not Set" });
        }

        [HttpGet("online-count")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult GetOnlineCount()
        {
            return Ok(new { onlineCount = NotificationHub.ConnectedUsers });
        }

        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _dbContext.Users.CountAsync();
                var totalInstitutes = await _dbContext.Institutes.CountAsync();
                var totalTutors = await _dbContext.Tutors.CountAsync();
                return Ok(new { totalUsers, totalInstitutes, totalTutors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load dashboard stats.", error = ex.Message });
            }
        }

        [HttpGet("students")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetStudents([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _studentService.GetAllStudentsAsync(searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("tutors")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetTutors([FromQuery] Guid? instituteId, [FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (instituteId.HasValue && instituteId.Value != Guid.Empty)
            {
                var result = await _instituteService.GetAssignedTutorsAsync(instituteId.Value, searchQuery, page, pageSize);
                if (!result.Success) return BadRequest(result);
                return Ok(result.Data);
            }
            else
            {
                var result = await _tutorService.GetAllTutorsAsync(searchQuery, page, pageSize);
                if (!result.Success) return BadRequest(result);
                return Ok(result.Data);
            }
        }

        [HttpGet("institutes")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetInstitutes([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _instituteService.GetAllInstitutesAsync(searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        public class ForceLogoutRequest
        {
            public string VersionNumber { get; set; } = string.Empty;
            public string ReleaseNotes { get; set; } = string.Empty;
        }

        [HttpPost("force-logout")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest request)
        {
            try
            {
                // 1. Update MinTokenDate to kill existing offline tokens upon next API request
                var setting = await _dbContext.AppSettings.FindAsync("MinTokenDate");
                if (setting == null)
                {
                    setting = new AppSetting { Key = "MinTokenDate", Value = DateTime.UtcNow.ToString("O") };
                    _dbContext.AppSettings.Add(setting);
                }
                else
                {
                    setting.Value = DateTime.UtcNow.ToString("O");
                    setting.UpdatedAt = DateTime.UtcNow;
                    _dbContext.AppSettings.Update(setting);
                }
                await _dbContext.SaveChangesAsync();

                // 2. Insert ONE System Announcement instead of looping millions of rows
                var title = $"System Update {request.VersionNumber} 🚀";
                var message = string.IsNullOrWhiteSpace(request.ReleaseNotes) 
                                ? "A new version of Tutorz is now live! Please enjoy the new features." 
                                : request.ReleaseNotes;

                var announcement = new Tutorz.Domain.Entities.SystemAnnouncement
                {
                    Title = title,
                    Message = message,
                    Type = "SystemUpdate",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.SystemAnnouncements.Add(announcement);
                await _dbContext.SaveChangesAsync();

                // 3. Broadcast SignalR event to all connected clients for instant logout
                await _notificationPusher.BroadcastToAllAsync(new { 
                    type = "ForceLogout", 
                    version = request.VersionNumber 
                });

                return Ok(new { message = "Force logout triggered and notifications sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to force logout.", error = ex.Message });
            }
        }

        [HttpPost("admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateAdmin([FromBody] Tutorz.Application.DTOs.System.CreateAdminDto request)
        {
            try
            {
                var result = await _authService.CreateAdminAsync(request);
                if (!result.Success) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create admin.", error = ex.Message });
            }
        }
        [HttpGet("admin/profile")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAdminProfile()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

            var result = await _adminService.GetAdminProfileAsync(userId);
            if (!result.Success) return NotFound(result.Message);
            return Ok(result);
        }

        [HttpPut("admin/profile")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateAdminProfile([FromForm] Tutorz.Application.DTOs.Admin.UpdateAdminProfileDto request)
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

            var result = await _adminService.UpdateAdminProfileAsync(userId, request);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        // --- Billing Config Endpoints ---

        [HttpGet("billing-config")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetBillingConfig()
        {
            var response = await _billService.GetBillingConfigAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("billing-config")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateBillingConfig([FromBody] BillingConfigDto config)
        {
            var response = await _billService.UpdateBillingConfigAsync(config);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // --- System Data Endpoints ---

        [HttpGet("payments-history")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetSystemPaymentsHistory(
            [FromQuery] Guid? instituteId,
            [FromQuery] Guid? tutorId,
            [FromQuery] Guid? classId,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _paymentService.GetAllSystemPaymentHistoryAsync(
                instituteId, tutorId, classId, searchQuery, page, pageSize);

            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("classes")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetSystemClasses([FromQuery] Guid? instituteId, [FromQuery] Guid? tutorId)
        {
            var query = _dbContext.Classes
                .AsNoTracking()
                .Include(c => c.Tutor)
                .Include(c => c.Institute)
                .AsQueryable();

            if (instituteId.HasValue && instituteId.Value != Guid.Empty)
            {
                query = query.Where(c => c.InstituteId == instituteId.Value);
            }
            if (tutorId.HasValue && tutorId.Value != Guid.Empty)
            {
                query = query.Where(c => c.TutorId == tutorId.Value);
            }

            var classes = await query
                .Select(c => new
                {
                    c.ClassId,
                    c.ClassName,
                    c.Subject,
                    c.Grade,
                    c.ClassType,
                    TutorName = c.Tutor != null ? c.Tutor.FirstName + " " + c.Tutor.LastName : null,
                    InstituteName = c.Institute != null ? c.Institute.InstituteName : null,
                    c.StartTime,
                    c.EndTime,
                    c.DayOfWeek,
                    c.Date,
                    c.HallName,
                    c.Fee,
                    c.InstituteCommissionRate,
                    c.IsActive,
                    StudentCount = c.Enrollments.Count
                })
                .ToListAsync();

            return Ok(classes);
        }
    }
}
