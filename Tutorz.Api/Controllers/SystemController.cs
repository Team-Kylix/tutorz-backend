using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using Tutorz.Domain.Entities;
using Tutorz.Api.Hubs;

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

        public SystemController(
            IConfiguration configuration,
            TutorzDbContext dbContext,
            INotificationPusher notificationPusher,
            IStudentService studentService,
            ITutorService tutorService,
            IInstituteService instituteService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _notificationPusher = notificationPusher;
            _studentService = studentService;
            _tutorService = tutorService;
            _instituteService = instituteService;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = _configuration["AppVersion"] ?? "0.0.0-unknown";
            return Ok(new { version });
        }

        [HttpGet("min-token-date")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMinTokenDate()
        {
            var setting = await _dbContext.AppSettings.FindAsync("MinTokenDate");
            return Ok(new { minTokenDate = setting?.Value ?? "Not Set" });
        }

        [HttpGet("online-count")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOnlineCount()
        {
            return Ok(new { onlineCount = NotificationHub.ConnectedUsers });
        }

        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudents([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _studentService.GetAllStudentsAsync(searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("tutors")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTutors([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _tutorService.GetAllTutorsAsync(searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("institutes")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

                // 2. Bulk insert SystemUpdate Notification to all users using Raw SQL for performance
                var title = $"System Update {request.VersionNumber} 🚀";
                var message = string.IsNullOrWhiteSpace(request.ReleaseNotes) 
                                ? "A new version of Tutorz is now live! Please enjoy the new features." 
                                : request.ReleaseNotes;

                // Executing Raw SQL to avoid loading all users into memory 
                // NEWID() generates a unique Guid for each notification.
                var sql = @"
                    INSERT INTO Notifications (NotificationId, UserId, Title, Message, [Type], IsRead, CreatedAt)
                    SELECT NEWID(), UserId, {0}, {1}, 'SystemUpdate', 0, GETUTCDATE()
                    FROM Users;
                ";
                
                await _dbContext.Database.ExecuteSqlRawAsync(sql, title, message);

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
    }
}
