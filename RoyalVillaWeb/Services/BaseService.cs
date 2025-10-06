using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;

namespace RoyalVillaWeb.Services
{
    public class BaseService : IBaseService
    {
        public IHttpClientFactory httpClient { get; set; }
        public ApiResponse<object> ResponseModel { get; set; }

        public BaseService(IHttpClientFactory httpClient)
        {
            this.ResponseModel = new();
            this.httpClient = httpClient;
        }

        public Task<T> SendAsync<T>(ApiRequest apiRequest)
        {
            throw new NotImplementedException();
        }
    }
}
