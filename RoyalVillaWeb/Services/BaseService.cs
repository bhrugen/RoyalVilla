using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Connections;
using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoyalVillaWeb.Services
{
    public class BaseService : IBaseService
    {
        public IHttpClientFactory _httpClient { get; set; }
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenProvider _tokenProvider;
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

        private bool IsRefreshingToken
        {
            get => _httpContextAccessor.HttpContext?.Session.GetString(RefreshingTokenKey) == "true";
            set
            {
                if (value)
                {
                    _httpContextAccessor.HttpContext?.Session.SetString(RefreshingTokenKey,"true");
                }
                else
                {
                    _httpContextAccessor.HttpContext?.Session.Remove(RefreshingTokenKey);
                }
            }
        }

        public async Task<T?> SendAsync<T>(ApiRequest apiRequest, bool withBearer=true)
        {
            try
            {
                var client = _httpClient.CreateClient("RoyalVillaAPI");

                var message = CreateRequestMessage(apiRequest, withBearer);
                var apiResponse = await client.SendAsync(message);

                //401 then we can try with refresh token
                if(apiResponse.StatusCode==HttpStatusCode.Unauthorized && withBearer && !IsRefreshingToken)
                {
                    Console.WriteLine("⚠️ Received401 Unauthorized - attempting token refresh");
                    var refreshed = await RefreshAccessTokenAsync();
                    if (refreshed)
                    {
                        //Token refreshed successfully - retrying request
                        var retryMessage = CreateRequestMessage(apiRequest, withBearer);
                        apiResponse = await client.SendAsync(retryMessage);
                    }
                    else
                    {
                        _tokenProvider.ClearToken();
                        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        _httpContextAccessor.HttpContext?.Response.Redirect("/auth/login");
                        return default;
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

                if (IsRefreshingToken)
                {
                    await Task.Delay(1000);
                    var accessToken = _tokenProvider.GetAccessToken();
                    if (accessToken != null)
                    {
                        return true;
                    }
                    return false;
                }
                IsRefreshingToken = true;

                var refreshToken = _tokenProvider.GetRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                var client = _httpClient.CreateClient("RoyalVillaAPI");
                var refreshRequest = new RefreshTokenRequestDTO
                {
                    RefreshToken = refreshToken
                };
                var apiRequest = new ApiRequest
                {
                    ApiType = SD.ApiType.POST,
                    Data = refreshRequest,
                    Url = "/api/auth/refresh-token",
                };

                var message = CreateRequestMessage(apiRequest, withBearer: false);
                var response = await client.SendAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenDTO>>();
                    if(result?.Success==true && result.Data!=null &&
                         !string.IsNullOrEmpty(result.Data.AccessToken) &&
        !string.IsNullOrEmpty(result.Data.RefreshToken))
                    {
                        //update tokens
                        _tokenProvider.SetToken(result.Data.AccessToken, result.Data.RefreshToken);
                        return true;
                    }
                }

                _tokenProvider.ClearToken();
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token refresh error: {ex.Message}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
                _tokenProvider.ClearToken();
                return false;
            }
            finally
            {
                IsRefreshingToken= false;
            }
        }

        private HttpRequestMessage CreateRequestMessage(ApiRequest apiRequest, bool withBearer)
        {
            var message = new HttpRequestMessage
            {
                RequestUri = new Uri(apiRequest.Url, uriKind: UriKind.Relative),
                Method = GetHttpMethod(apiRequest.ApiType),
            };


            var token = _tokenProvider.GetAccessToken();
            if (withBearer && !string.IsNullOrEmpty(token))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

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

            return message;
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
