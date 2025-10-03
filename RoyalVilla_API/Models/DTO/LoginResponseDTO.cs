namespace RoyalVilla_API.Models.DTO
{
    public class LoginResponseDTO
    {

        public string? Token { get; set; }

        public UserDTO? UserDTO { get; set; }
    }
}
