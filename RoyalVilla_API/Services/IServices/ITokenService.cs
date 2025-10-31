using RoyalVilla_API.Models;

namespace RoyalVilla_API.Services.IServices
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();

        Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt);

        Task<bool> RevokeRefreshTokenAsync(string refreshTokenId);


        Task<(bool IsValid, string? UserId, string? TokenFamilyId, bool TokenReused)> ValidateRefreshTokenAsync(string refreshToken);
    }
}
