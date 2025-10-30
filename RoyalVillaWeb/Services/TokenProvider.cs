using RoyalVillaWeb.Services.IServices;
using System.Security.Claims;

namespace RoyalVillaWeb.Services
{
    public class TokenProvider : ITokenProvider
    {
        public void ClearToken()
        {
            throw new NotImplementedException();
        }

        public ClaimsPrincipal? GetClaimsFromToken(string token)
        {
            throw new NotImplementedException();
        }

        public string? GetToken()
        {
            throw new NotImplementedException();
        }

        public void SetToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
