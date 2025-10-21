using RoyalVilla_API.Models;
using System.Security.Claims;

namespace RoyalVilla_API.Services
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt);
        Task<(bool IsValid, string? UserId, string? TokenFamilyId, bool TokenReused)> ValidateRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task RevokeTokenFamilyAsync(string jwtTokenId, string userId);
        Task RevokeAllUserTokensAsync(string userId);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
