using System.ComponentModel.DataAnnotations;

namespace RoyalVilla.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }

        public string Email { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string Role { get; set; } = default!;
    }
}
