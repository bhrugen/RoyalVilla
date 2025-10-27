using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RoyalVilla.DTO
{
    public class VillaCreateDTO
    {
        [MaxLength(50)]
        [Required]
        public required string Name { get; set; }
        public string? Details { get; set; }
        public double Rate { get; set; }
        public int? Sqft { get; set; }
        public int? Occupancy { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }

    }
}
