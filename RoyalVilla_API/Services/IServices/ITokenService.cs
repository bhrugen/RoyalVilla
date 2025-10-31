using RoyalVilla_API.Models;

namespace RoyalVilla_API.Services.IServices
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();

        Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt);

        Task RevokeRefreshTokenAsync(string jwTokenId, string userId);
    }
}
