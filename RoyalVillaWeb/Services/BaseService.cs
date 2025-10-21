using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;
using System.Net.Http.Headers;
using System.Net;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        // Session key for refresh lock - prevents concurrent refresh requests
        private const string RefreshingTokenKey = "_RefreshingToken";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiResponse<object> ResponseModel { get; set; }

        public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor)
        {
            this.ResponseModel = new();
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        // Session-based property - shared across all BaseService instances for same user
        private bool IsRefreshingToken
        {
            get => _httpContextAccessor.HttpContext?.Session.GetString(RefreshingTokenKey) == "true";
            set
            {
                if (value)
                {
                    _httpContextAccessor.HttpContext?.Session.SetString(RefreshingTokenKey, "true");
                }
                else
                {
                    _httpContextAccessor.HttpContext?.Session.Remove(RefreshingTokenKey);
                }
            }
        }

        public async Task<T?> SendAsync<T>(ApiRequest apiRequest, bool withBearer = true)
        {
            try
            {
                var client = _httpClient.CreateClient("RoyalVillaAPI");
                var message = new HttpRequestMessage
                {
                    RequestUri = new Uri(apiRequest.Url, uriKind: UriKind.Relative),
                    Method = GetHttpMethod(apiRequest.ApiType),
                };

                // Use TokenProvider to get the access token
                var accessToken = _tokenProvider.GetAccessToken();
                if (withBearer && !string.IsNullOrEmpty(accessToken))
                {
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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

                // ✅ ONLY refresh token if we get 401 Unauthorized
                if (apiResponse.StatusCode == HttpStatusCode.Unauthorized && withBearer && !IsRefreshingToken)
                {
                    Console.WriteLine("⚠️ Received 401 Unauthorized - attempting token refresh");
                    
                    var refreshed = await RefreshAccessTokenAsync();
                    if (refreshed)
                    {
                        Console.WriteLine("✅ Token refreshed successfully - retrying request");
                        
                        // Retry the request with new token
                        var newAccessToken = _tokenProvider.GetAccessToken();
                        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        
                        // Recreate content if needed (streams can only be read once)
                        if (apiRequest.Data != null)
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
                        
                        apiResponse = await client.SendAsync(message);
                    }
                    else
                    {
                        Console.WriteLine("❌ Token refresh failed - user needs to login again");
                    }
                }

                return await apiResponse.Content.ReadFromJsonAsync<T>(JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return default;
            }
        }

        private async Task<bool> RefreshAccessTokenAsync()
        {
            try
            {
                // Session-based lock - prevents multiple concurrent refresh requests
                if (IsRefreshingToken)
                {
                    // Another request is already refreshing, wait a bit
                    Console.WriteLine("⏳ Another request is already refreshing token, waiting...");
                    await Task.Delay(1000); // Wait 1 second
                    
                    // Check if token was updated by the other request
                    var accessToken = _tokenProvider.GetAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Console.WriteLine("✅ Token was refreshed by another request");
                        return true;
                    }
                    
                    Console.WriteLine("❌ Token still not available after waiting");
                    return false;
                }

                IsRefreshingToken = true; // Set session lock

                var refreshToken = _tokenProvider.GetRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine("❌ No refresh token available");
                    return false;
                }

                var client = _httpClient.CreateClient("RoyalVillaAPI");
                var refreshRequest = new RefreshTokenRequestDTO
                {
                    RefreshToken = refreshToken
                };

                var message = new HttpRequestMessage
                {
                    RequestUri = new Uri("/api/auth/refresh-token", UriKind.Relative),
                    Method = HttpMethod.Post,
                    Content = JsonContent.Create(refreshRequest, options: JsonOptions)
                };

                Console.WriteLine("🔄 Calling refresh token API...");
                var response = await client.SendAsync(message);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenDTO>>(JsonOptions);
                    
                    if (result?.Success == true && result.Data != null && 
                        !string.IsNullOrEmpty(result.Data.AccessToken) && 
                        !string.IsNullOrEmpty(result.Data.RefreshToken))
                    {
                        // Update tokens
                        _tokenProvider.SetToken(result.Data.AccessToken, result.Data.RefreshToken);
                        
                        Console.WriteLine("✅ Tokens updated successfully");
                        return true;
                    }
                }

                // Refresh failed - clear tokens
                _tokenProvider.ClearToken();
                Console.WriteLine($"❌ Token refresh failed with status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token refresh error: {ex.Message}");
                _tokenProvider.ClearToken();
                return false;
            }
            finally
            {
                IsRefreshingToken = false; // Clear session lock
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
