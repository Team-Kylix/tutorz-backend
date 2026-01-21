using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Institute")] // Only Institutes can access
    public class InstituteController : ControllerBase
    {
        private readonly IInstituteService _instituteService;

        public InstituteController(IInstituteService instituteService)
        {
            _instituteService = instituteService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // FIX: Look for "InstituteId" claim first
            var idString = User.FindFirst("InstituteId")?.Value;

            // Fallback to NameIdentifier if needed
            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _instituteService.GetProfileAsync(instituteId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateInstituteProfileDto dto)
        {
            var idString = User.FindFirst("InstituteId")?.Value;

            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _instituteService.UpdateProfileAsync(instituteId, dto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}