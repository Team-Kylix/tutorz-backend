using Microsoft.AspNetCore.Mvc;

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

        public SystemController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = _configuration["AppVersion"] ?? "0.0.0-unknown";
            return Ok(new { version });
        }
    }
}
