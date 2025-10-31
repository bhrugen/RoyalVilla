using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using RoyalVilla_API.Services.IServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RoyalVilla_API.Services
{
    public class TokenService : ITokenService
    {

        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public TokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _configuration = configuration;
            _userManager = userManager;
            _db = db;
        }


        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("JwtSettings")["Secret"]);
            var roles = await _userManager.GetRolesAsync(user);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                    new Claim(ClaimTypes.Email,user.Email),
                    new Claim(ClaimTypes.Name,user.Name),
                    new Claim(ClaimTypes.Role,roles.FirstOrDefault()),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> GenerateRefreshTokenAsync()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            var exists = await _db.RefreshTokens.AnyAsync(u=>u.RefreshTokenValue== refreshToken);
            if (exists)
            {
                return await GenerateRefreshTokenAsync();
            }

            return refreshToken;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshTokenId)
        {
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.RefreshTokenValue == refreshTokenId);
            if (storedToken == null)
            {
                return false;
            }
            storedToken.IsValid = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task SaveRefreshTokenAsync(string userId, string jwtTokenId, string refreshToken, DateTime expiresAt)
        {
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                JwtTokenId = jwtTokenId,
                ExpiresAt = expiresAt,
                RefreshTokenValue = refreshToken,
                IsValid = true
            };
            await _db.RefreshTokens.AddAsync(refreshTokenEntity);
            await _db.SaveChangesAsync();
        }

        public async Task<(bool IsValid, string? UserId, string? TokenFamilyId, bool TokenReused)> ValidateRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.RefreshTokenValue == refreshToken);

            // Token doesn't exist in database
            if (storedToken == null)
            {
                return (false, null, null, false);
            }

            // CRITICAL SECURITY CHECK: Token Reuse Detection
            // If token exists but is marked as invalid, it means someone tried to reuse it
            // This is a strong indicator of token theft
            if (!storedToken.IsValid)
            {
                // Revoke all tokens in THIS TOKEN FAMILY

                return (false, storedToken.UserId, storedToken.JwtTokenId, true);
            }

            if (storedToken.ExpiresAt < DateTime.Now)
            {
                return (false, storedToken.UserId, storedToken.JwtTokenId, false);
            }

            return (true, storedToken.UserId, storedToken.JwtTokenId, false);

        }
    }
}
