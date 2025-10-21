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

        public AuthService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IMapper mapper)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _mapper = mapper;
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
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Long-lived refresh token

                // Save refresh token to database
                if (!string.IsNullOrEmpty(jwtTokenId))
                {
                    await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, refreshToken, refreshTokenExpiry);
                }

                return new TokenDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (Exception ex)
            {
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

                // Validate refresh token
                var (isValid, userId) = await _tokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
                if (!isValid || string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                // Get user
                var user = await _db.ApplicationUsers.FindAsync(userId);
                if (user == null)
                {
                    return null;
                }

                // Revoke old refresh token
                await _tokenService.RevokeRefreshTokenAsync(refreshTokenRequest.RefreshToken);

                // Generate new JWT access token
                var accessToken = await _tokenService.GenerateJwtTokenAsync(user);
                
                // Extract JWT ID from new access token
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                // Generate new refresh token
                var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Save new refresh token
                if (!string.IsNullOrEmpty(jwtTokenId))
                {
                    await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, newRefreshToken, refreshTokenExpiry);
                }

                return new TokenDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (Exception ex)
            {
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

                return userDto;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred during user registration", ex);
            }
        }
    }
}
