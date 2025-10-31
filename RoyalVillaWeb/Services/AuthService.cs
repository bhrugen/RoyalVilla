using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;

namespace RoyalVillaWeb.Services
{
    public class AuthService : BaseService, IAuthService
    {

        private const string APIEndpoint = "/api/auth";
        public AuthService(IHttpClientFactory httpClient, IConfiguration configuration, ITokenProvider tokenProvider) 
            : base(httpClient,tokenProvider)
        {
        }

        public Task<T?> LoginAsync<T>(LoginRequestDTO loginRequestDTO)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = loginRequestDTO,
                Url = APIEndpoint+"/login",
            },withBearer:false);
        }

        public Task<T?> RegisterAsync<T>(RegisterationRequestDTO registerationRequestDTO)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = registerationRequestDTO,
                Url = APIEndpoint+ "/register",
            }, withBearer: false);
        }
    }
}
