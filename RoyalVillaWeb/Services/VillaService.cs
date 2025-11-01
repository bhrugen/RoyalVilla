using RoyalVilla.DTO;
using RoyalVillaWeb.Extensions;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;

namespace RoyalVillaWeb.Services
{
    public class VillaService : BaseService,IVillaService
    {
        
        private const string APIEndpoint = $"/api/{SD.CurrentAPIVersion}/villa";
        public VillaService(IHttpClientFactory httpClient, IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration, ITokenProvider tokenProvider) 
            : base(httpClient,tokenProvider)
        {
        }

        public Task<T?> CreateAsync<T>(VillaCreateDTO dto)
        {

            var formData = dto.ToMultipartFormData();
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.POST,
                Data = formData,
                Url = APIEndpoint
            });
        }

        public Task<T?> DeleteAsync<T>(int id)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.DELETE,
                Url = $"{APIEndpoint}/{id}"
            });
        }

        public Task<T?> GetAllAsync<T>()
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.GET,
                Url = $"{APIEndpoint}"
            });
        }

        public Task<T?> GetAsync<T>(int id)
        {
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.GET,
             
                Url = $"{APIEndpoint}/{id}"
            });
        }

        public Task<T?> UpdateAsync<T>(VillaUpdateDTO dto)
        {
            var formData = dto.ToMultipartFormData();
            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.PUT,
                Data = formData,
                Url = $"{APIEndpoint}/{dto.Id}"
            });
        }
    }
}
