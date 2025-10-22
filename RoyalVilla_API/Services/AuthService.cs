using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoyalVilla.DTO;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RoyalVilla_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<AuthService> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _db.ApplicationUsers.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<TokenDTO?> LoginAsync(LoginRequestDTO loginRequestDTO)
        {
            try
            {
                // Get user by email
                var user = await _db.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginRequestDTO.Email.ToLower());

                if (user == null)
                {
                    return null; // User not found
                }

                // Validate password
                bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
                if (!isValid)
                {
                    return null; // Invalid password
                }

                // Generate JWT access token
                var accessToken = await _tokenService.GenerateJwtTokenAsync(user);
                
                // Extract JWT ID from access token
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                // Generate refresh token
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(5); // Long-lived refresh token

                // Save refresh token to database
                if (!string.IsNullOrEmpty(jwtTokenId))
                {
                    await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, refreshToken, refreshTokenExpiry);
                }

                _logger.LogInformation("User logged in successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

                return new TokenDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", loginRequestDTO.Email);
                throw new InvalidOperationException("An unexpected error occurred during user login", ex);
            }
        }

        public async Task<TokenDTO?> RefreshTokenAsync(RefreshTokenRequestDTO refreshTokenRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshTokenRequest.RefreshToken))
                {
                    return null;
                }

                // Validate refresh token with reuse detection
                var (isValid, userId, tokenFamilyId, tokenReused) = await _tokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
                
                // 🚨 CRITICAL SECURITY: Token Reuse Detected
                if (tokenReused)
                {
                    _logger.LogWarning(
                        "🚨 SECURITY BREACH: Token reuse attempt detected for UserId: {UserId}, TokenFamily: {TokenFamilyId}. " +
                        "All tokens in this family have been revoked. User must login again.", 
                        userId, tokenFamilyId);
                    
                    // Return null - user must re-authenticate
                    // All tokens in this family have already been revoked in ValidateRefreshTokenAsync
                    return null;
                }

                // Token is invalid or expired (but not reused)
                if (!isValid || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenFamilyId))
                {
                    return null;
                }

                // Get user
                var user = await _db.ApplicationUsers.FindAsync(userId);
                if (user == null)
                {
                    return null;
                }

                // Revoke old refresh token (normal token rotation)
                await _tokenService.RevokeRefreshTokenAsync(refreshTokenRequest.RefreshToken);

                // Generate new JWT access token
                var accessToken = await _tokenService.GenerateJwtTokenAsync(user);

                // Generate new refresh token
                var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(5);

                // 🔑 KEY CHANGE: Use the SAME tokenFamilyId to maintain the token chain
                // This allows tracking of token families and selective revocation
                await _tokenService.SaveRefreshTokenAsync(user.Id, tokenFamilyId, newRefreshToken, refreshTokenExpiry);

                _logger.LogInformation(
                    "Token refreshed successfully. UserId: {UserId}, TokenFamily: {TokenFamilyId}", 
                    user.Id, tokenFamilyId);

                // Extract token expiry from new access token
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);

                return new TokenDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh");
                throw new InvalidOperationException("An unexpected error occurred during token refresh", ex);
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                return await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token revocation");
                throw new InvalidOperationException("An unexpected error occurred during token revocation", ex);
            }
        }

        public async Task<UserDTO?> RegisterAsync(RegisterationRequestDTO registerationRequestDTO)
        {
            try
            {
                // Check if email already exists
                if (await IsEmailExistsAsync(registerationRequestDTO.Email))
                {
                    throw new InvalidOperationException(
                        $"User with email '{registerationRequestDTO.Email}' already exists");
                }

                // Create user
                var user = new ApplicationUser
                {
                    Email = registerationRequestDTO.Email,
                    Name = registerationRequestDTO.Name,
                    UserName = registerationRequestDTO.Email,
                    NormalizedEmail = registerationRequestDTO.Email.ToUpper(),
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"User registration failed: {errors}");
                }

                // Assign role
                var role = string.IsNullOrEmpty(registerationRequestDTO.Role)
                    ? "Customer"
                    : registerationRequestDTO.Role;

                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }

                await _userManager.AddToRoleAsync(user, role);

                // Map to DTO
                var userDto = _mapper.Map<UserDTO>(user);
                userDto.Role = role;

                _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}, Role: {Role}", 
                    user.Id, user.Email, role);

                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for email: {Email}", registerationRequestDTO.Email);
                throw new InvalidOperationException("An unexpected error occurred during user registration", ex);
            }
        }
    }
}
