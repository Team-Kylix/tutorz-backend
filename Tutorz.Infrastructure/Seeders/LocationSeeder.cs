using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Seeders
{
    public class LocationSeeder
    {
        private readonly TutorzDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LocationSeeder(TutorzDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task SeedAsync()
        {
            // Check if data already exists
            if (await _context.Provinces.AnyAsync()) return;

            // Define File Path
            var filePath = Path.Combine(_env.WebRootPath, "cities-by-district.json");
            if (!File.Exists(filePath)) return;

            // Read & Parse JSON
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var districtData = JsonSerializer.Deserialize<Dictionary<string, DistrictJsonModel>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Manual Province Mapping (Since JSON lacks Provinces)
            var provinceMap = new Dictionary<string, List<string>>
            {
                { "Western", new() { "Colombo", "Gampaha", "Kalutara" } },
                { "Central", new() { "Kandy", "Matale", "Nuwara Eliya" } },
                { "Southern", new() { "Galle", "Matara", "Hambantota" } },
                { "Northern", new() { "Jaffna", "Kilinochchi", "Mannar", "Vavuniya", "Mullaitivu" } },
                { "Eastern", new() { "Ampara", "Batticaloa", "Trincomalee" } },
                { "North Western", new() { "Kurunegala", "Puttalam" } },
                { "North Central", new() { "Anuradhapura", "Polonnaruwa" } },
                { "Uva", new() { "Badulla", "Monaragala" } },
                { "Sabaragamuwa", new() { "Ratnapura", "Kegalle" } }
            };

            // Build and Save
            foreach (var provEntry in provinceMap)
            {
                var province = new Province { Name = provEntry.Key, Districts = new List<District>() };

                foreach (var distName in provEntry.Value)
                {
                    var district = new District { Name = distName, Cities = new List<City>() };

                    // Find cities in JSON matching this district name
                    var jsonKey = districtData.Keys.FirstOrDefault(k => k.Equals(distName, StringComparison.OrdinalIgnoreCase));
                    if (jsonKey != null && districtData.TryGetValue(jsonKey, out var cityList))
                    {
                        if (cityList.cities != null)
                        {
                            foreach (var cityName in cityList.cities)
                            {
                                district.Cities.Add(new City { Name = cityName });
                            }
                        }
                    }
                    province.Districts.Add(district);
                }
                _context.Provinces.Add(province);
            }

            await _context.SaveChangesAsync();
        }

        private class DistrictJsonModel
        {
            public List<string> cities { get; set; }
        }
    }
}