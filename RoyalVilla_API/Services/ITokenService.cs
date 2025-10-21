using RoyalVilla_API.Models;
using System.Security.Claims;

namespace RoyalVilla_API.Services
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        ClaimsPrincipal? ValidateToken(string token);
        Task<(bool IsValid, string? UserId)> ValidateRefreshTokenAsync(string refreshToken);
        Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    }
}
