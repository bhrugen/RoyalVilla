using Microsoft.AspNetCore.Authentication.Cookies;
using RoyalVillaWeb.Services.IServices;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace RoyalVillaWeb.Services
{
    public class TokenProvider : ITokenProvider
    {

        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor=httpContextAccessor;
        }

        public void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(SD.SessionToken);
        }

        public ClaimsPrincipal? CreatePrincipalFromJwtToken(string token)
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

                var emailClaim = jwt.Claims.FirstOrDefault(u => u.Type == "email");
                if (emailClaim != null) 
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, emailClaim.Value));
                }

                var roleClaim = jwt.Claims.FirstOrDefault(u => u.Type == "role");
                if (roleClaim != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, roleClaim.Value));
                }

                var nameClaim = jwt.Claims.FirstOrDefault(u => u.Type == "name");
                if (nameClaim != null)
                {
                    identity.AddClaim(new Claim("FullName", nameClaim.Value));
                }


                return new ClaimsPrincipal(identity);

            }
            catch (Exception ex) 
            {
                return null;
            }
        }

        public string? GetToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(SD.SessionToken);
        }

        public void SetToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(SD.SessionToken, token);
        }
    }
}
