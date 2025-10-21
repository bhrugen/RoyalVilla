using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RoyalVillaWeb.Services.IServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RoyalVillaWeb.Services
{
    public class TokenProvider : ITokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetToken(string accessToken, string refreshToken)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(SD.SessionToken, accessToken);
            _httpContextAccessor.HttpContext?.Session.SetString(SD.SessionRefreshToken, refreshToken);
        }

        public string? GetAccessToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(SD.SessionToken);
        }

        public string? GetRefreshToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(SD.SessionRefreshToken);
        }

        public void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(SD.SessionToken);
            _httpContextAccessor.HttpContext?.Session.Remove(SD.SessionRefreshToken);
        }

        public ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

                // Extract claims from JWT
                var emailClaim = jwt.Claims.FirstOrDefault(u => u.Type == "email");
                if (emailClaim != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, emailClaim.Value));
                }

                var roleClaim = jwt.Claims.FirstOrDefault(u => u.Type == "role");
                if (roleClaim != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                }

                var nameClaim = jwt.Claims.FirstOrDefault(u => u.Type == "name");
                if (nameClaim != null)
                {
                    identity.AddClaim(new Claim("FullName", nameClaim.Value));
                }

                var nameIdClaim = jwt.Claims.FirstOrDefault(u => u.Type == "nameid");
                if (nameIdClaim != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdClaim.Value));
                }

                return new ClaimsPrincipal(identity);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
