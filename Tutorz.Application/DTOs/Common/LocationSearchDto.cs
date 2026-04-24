using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Common
{
    public class LocationSearchResponseDto
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public List<DistrictSearchResultDto> Districts { get; set; } = new List<DistrictSearchResultDto>();
    }

    public class DistrictSearchResultDto
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public List<CitySearchResultDto> Cities { get; set; } = new List<CitySearchResultDto>();
    }

    public class CitySearchResultDto
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
    }
}
