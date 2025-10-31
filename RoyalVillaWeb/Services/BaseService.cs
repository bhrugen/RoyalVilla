using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoyalVillaWeb.Services
{
    public class BaseService : IBaseService
    {
        public IHttpClientFactory _httpClient { get; set; }
        private readonly ITokenProvider _tokenProvider;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiResponse<object> ResponseModel { get; set; }

        public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider)
        {
            this.ResponseModel = new();
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
        }

        public async Task<T?> SendAsync<T>(ApiRequest apiRequest, bool withBearer=true)
        {
            try
            {
                var client = _httpClient.CreateClient("RoyalVillaAPI");
                var message = new HttpRequestMessage
                {
                    RequestUri = new Uri(apiRequest.Url,uriKind:UriKind.Relative),
                    Method = GetHttpMethod(apiRequest.ApiType),
                };


                var token = _tokenProvider.GetToken();
                if (withBearer && !string.IsNullOrEmpty(token))
                {
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if(apiRequest.Data!=null)
                {
                    if (apiRequest.Data is MultipartFormDataContent multipartContent)
                    {
                        message.Content = multipartContent;
                    }
                    else
                    {
                        message.Content = JsonContent.Create(apiRequest.Data, options: JsonOptions);
                    }
                }

                var apiResponse = await client.SendAsync(message);

                return await apiResponse.Content.ReadFromJsonAsync<T>(JsonOptions);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return default;
            }
        }

        private static HttpMethod GetHttpMethod(SD.ApiType apiType) 
        {
            return apiType switch
            {
                SD.ApiType.POST => HttpMethod.Post,
                SD.ApiType.PUT => HttpMethod.Put,
                SD.ApiType.DELETE => HttpMethod.Delete,
                _ => HttpMethod.Get
            };
        }

    }
}
