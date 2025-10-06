using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalVilla.DTO
{
    public class VillaAmentiesCreateDTO
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        public string? Description { get; set; }
        [Required]
        public int VillaId { get; set; }
    }
}
