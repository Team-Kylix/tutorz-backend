using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Api.Controllers
{
    [Authorize(Roles = "Tutor")]
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly ITutorService _tutorService;

        public TutorController(ITutorService tutorService)
        {
            _tutorService = tutorService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
        }

        [HttpPost("classes")]
        public async Task<IActionResult> CreateClass(CreateClassRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.CreateClassAsync(userId, request);
            return Ok(result);
        }

        [HttpPut("classes/{id}")]
        public async Task<IActionResult> UpdateClass(Guid id, CreateClassRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.UpdateClassAsync(id, userId, request);
            return Ok(result);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetClassesAsync(userId);
            return Ok(result);
        }

        [HttpPost("classes/add-student")]
        public async Task<IActionResult> AddStudent(AddStudentRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _tutorService.AddStudentToClassAsync(userId, request);
            return Ok(new { message = "Student added successfully" });
        }

        [HttpDelete("classes/{id}")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _tutorService.DeleteClassAsync(id, userId);
            return Ok(new { message = "Class deleted successfully" });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var profile = await _tutorService.GetTutorProfileAsync(userId);
            if (profile == null) return NotFound("Profile not found");
            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] TutorProfileDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.UpdateTutorProfileAsync(userId, request);

            if (!result.Success) return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests()
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var requests = await _tutorService.GetStudentRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("requests/process")]
        public async Task<IActionResult> ProcessRequests([FromBody] ProcessRequestDto request)
        {
            try
            {
                var result = await _tutorService.ProcessStudentRequestsAsync(request);

                if (!result)
                    return BadRequest(new { message = "Failed to process requests. No valid pending requests found or invalid action." });

                return Ok(new { message = $"Requests {request.Action.ToLower()} successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("student-profile/{studentId}")]
        public async Task<IActionResult> GetStudentProfile(Guid studentId)
        {
            try
            {
                var profile = await _tutorService.GetStudentProfileAsync(studentId);

                if (profile == null)
                    return NotFound(new { message = "Student profile not found." });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}