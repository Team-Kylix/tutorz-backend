using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tutorz.Domain.Entities
{
    public class Province
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore] 
        public ICollection<District> Districts { get; set; }
    }

    public class District
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public int ProvinceId { get; set; }
        [ForeignKey("ProvinceId")]
        [JsonIgnore]
        public Province Province { get; set; }

        [JsonIgnore]
        public ICollection<City> Cities { get; set; }
    }

    public class City
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public int DistrictId { get; set; }
        [ForeignKey("DistrictId")]
        [JsonIgnore]
        public District District { get; set; }
    }
}