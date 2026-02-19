using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorz.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly TutorzDbContext _context;

        public LocationsController(TutorzDbContext context)
        {
            _context = context;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            return Ok(await _context.Provinces.Select(p => new { p.Id, p.Name }).ToListAsync());
        }

        [HttpGet("provinces/{provinceId}/districts")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            return Ok(await _context.Districts.Where(d => d.ProvinceId == provinceId).Select(d => new { d.Id, d.Name }).ToListAsync());
        }

        [HttpGet("districts/{districtId}/cities")]
        public async Task<IActionResult> GetCities(int districtId)
        {
            return Ok(await _context.Cities.Where(c => c.DistrictId == districtId).Select(c => new { c.Id, c.Name }).ToListAsync());
        }
    }
}