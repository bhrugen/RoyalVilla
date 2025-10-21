using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoyalVillaWeb.Services
{
    /*
      Follows SOLID Principles ✅
•	Single Responsibility: Each service has one reason to change
•	Open/Closed: Can extend ITokenProvider without modifying services
•	Liskov Substitution: Can swap TokenProvider implementations
•	Interface Segregation: ITokenProvider has focused methods
•	Dependency Inversion: Services depend on abstraction, not implementation

    Single Source of Truth ✅
•	All token operations go through TokenProvider
•	No direct session access scattered across services
•	Consistent token management


     What This Means:
Before:
•	Services had mixed concerns (HTTP + Session access)
•	Hard to test (mock multiple layers)
•	Inconsistent token access
After:
•	Clean separation of concerns
•	Easy to test (mock one interface)
•	Consistent token access through TokenProvider
•	All services use the same token management approach
    */
    public class BaseService : IBaseService
    {
        private readonly IHttpClientFactory _httpClient;
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
                    RequestUri = new Uri(apiRequest.Url, uriKind: UriKind.Relative),
                    Method = GetHttpMethod(apiRequest.ApiType),
                };

                // Use TokenProvider to get the token
                var token = _tokenProvider.GetToken();
                if (withBearer && !string.IsNullOrEmpty(token))
                {
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (apiRequest.Data != null)
                {
                    // Check if data is MultipartFormDataContent (for file uploads)
                    if (apiRequest.Data is MultipartFormDataContent multipartContent)
                    {
                        message.Content = multipartContent;
                    }
                    else
                    {
                        // Use JSON for regular data
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
