using RoyalVilla.DTO;

namespace RoyalVilla_API.Services
{
    public interface IAuthService
    {
        Task<UserDTO?> RegisterAsync(RegisterationRequestDTO registerationRequestDTO);

        Task<TokenDTO?> LoginAsync(LoginRequestDTO loginRequestDTO);

        Task<TokenDTO?> RefreshTokenAsync(RefreshTokenRequestDTO refreshTokenRequest);

        Task<bool> RevokeTokenAsync(string refreshToken);

        Task<bool> IsEmailExistsAsync(string email);
    }
}
