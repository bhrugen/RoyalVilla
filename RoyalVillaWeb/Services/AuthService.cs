using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;

namespace RoyalVillaWeb.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private const string APIEndpoint = "/api/auth";

        public AuthService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, tokenProvider, httpContextAccessor)
        {
        }

        public Task<T?> LoginAsync<T>(LoginRequestDTO loginRequestDTO)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = loginRequestDTO,
                Url = APIEndpoint + "/login",
            }, withBearer: false);  // ✅ No token needed - user is logging in to GET a token
        }

        public Task<T?> RegisterAsync<T>(RegisterationRequestDTO registerationRequestDTO)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = registerationRequestDTO,
                Url = APIEndpoint + "/register",
            }, withBearer: false);  // ✅ No token needed - user doesn't have an account yet
        }

        public Task<T?> RefreshTokenAsync<T>(RefreshTokenRequestDTO refreshTokenRequest)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = refreshTokenRequest,
                Url = APIEndpoint + "/refresh-token",
            }, withBearer: false);  // ✅ No bearer - using refresh token in body
        }
    }
}
