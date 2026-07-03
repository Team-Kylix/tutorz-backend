using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorz.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Api.Attributes;
using Tutorz.Application.DTOs.Common;
using System.Collections.Generic;

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
        [ApiPurpose("Get Provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            return Ok(await _context.Provinces.Select(p => new { p.Id, p.Name }).ToListAsync());
        }

        [HttpGet("provinces/{provinceId}/districts")]
        [ApiPurpose("Get Districts by Province")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            return Ok(await _context.Districts.Where(d => d.ProvinceId == provinceId).Select(d => new { d.Id, d.Name }).ToListAsync());
        }

        [HttpGet("districts/{districtId}/cities")]
        [ApiPurpose("Get Cities by District")]
        public async Task<IActionResult> GetCities(int districtId)
        {
            return Ok(await _context.Cities.Where(c => c.DistrictId == districtId).Select(c => new { c.Id, c.Name }).ToListAsync());
        }

        [HttpGet("search")]
        [ApiPurpose("Search Locations Hierarchically")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(new List<LocationSearchResponseDto>());

            var query = q.ToLower();

            // Find cities matching query OR districts matching query
            var cities = await _context.Cities
                .Include(c => c.District)
                    .ThenInclude(d => d.Province)
                .Where(c => c.Name.ToLower().Contains(query) || c.District.Name.ToLower().Contains(query))
                .OrderBy(c => c.District.Province.Name)
                .ThenBy(c => c.District.Name)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var groupedResults = cities
                .GroupBy(c => new { c.District.ProvinceId, c.District.Province.Name })
                .Select(pGroup => new LocationSearchResponseDto
                {
                    ProvinceId = pGroup.Key.ProvinceId,
                    ProvinceName = pGroup.Key.Name,
                    Districts = pGroup.GroupBy(c => new { c.DistrictId, c.District.Name })
                        .Select(dGroup => new DistrictSearchResultDto
                        {
                            DistrictId = dGroup.Key.DistrictId,
                            DistrictName = dGroup.Key.Name,
                            Cities = dGroup.Select(c => new CitySearchResultDto
                            {
                                CityId = c.Id,
                                CityName = c.Name
                            }).ToList()
                        }).ToList()
                }).ToList();

            return Ok(groupedResults);
        }
    }
}