using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RoyalVillaWeb.Services.IServices
{
    public interface ITokenProvider
    {
        void SetToken(string accessToken, string refreshToken);
        string? GetAccessToken();
        string? GetRefreshToken();
        void ClearToken();
        ClaimsPrincipal? GetPrincipalFromToken(string token);
    }
}
