using RoyalVilla.DTO;
using RoyalVillaWeb.Models;
using RoyalVillaWeb.Services.IServices;

namespace RoyalVillaWeb.Services
{
    public class VillaService : BaseService, IVillaService
    {
        private const string APIEndpoint = $"/api/{SD.CurrentAPIVersion}/villa";

        public VillaService(IHttpClientFactory httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(httpClient, httpContextAccessor)
        {
        }

        public Task<T?> CreateAsync<T>(VillaCreateDTO dto)
        {
            var formData = new MultipartFormDataContent();

            // Add all properties as form data
            formData.Add(new StringContent(dto.Name), "Name");

            if (!string.IsNullOrEmpty(dto.Details))
            {
                formData.Add(new StringContent(dto.Details), "Details");
            }

            formData.Add(new StringContent(dto.Rate.ToString()), "Rate");

            if (dto.Sqft.HasValue)
            {
                formData.Add(new StringContent(dto.Sqft.Value.ToString()), "Sqft");
            }

            if (dto.Occupancy.HasValue)
            {
                formData.Add(new StringContent(dto.Occupancy.Value.ToString()), "Occupancy");
            }

            if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                formData.Add(new StringContent(dto.ImageUrl), "ImageUrl");
            }

            // Add image file if present
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var streamContent = new StreamContent(dto.Image.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.Image.ContentType);
                formData.Add(streamContent, "Image", dto.Image.FileName);
            }

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
            var formData = new MultipartFormDataContent();

            // Add all properties as form data
            formData.Add(new StringContent(dto.Id.ToString()), "Id");
            formData.Add(new StringContent(dto.Name), "Name");

            if (!string.IsNullOrEmpty(dto.Details))
            {
                formData.Add(new StringContent(dto.Details), "Details");
            }

            formData.Add(new StringContent(dto.Rate.ToString()), "Rate");
            formData.Add(new StringContent(dto.Sqft.ToString()), "Sqft");
            formData.Add(new StringContent(dto.Occupancy.ToString()), "Occupancy");

            if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                formData.Add(new StringContent(dto.ImageUrl), "ImageUrl");
            }

            // Add image file if present
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var streamContent = new StreamContent(dto.Image.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.Image.ContentType);
                formData.Add(streamContent, "Image", dto.Image.FileName);
            }

            return SendAsync<T>(new ApiRequest
            {
                ApiType = SD.ApiType.PUT,
                Data = formData,
                Url = $"{APIEndpoint}/{dto.Id}"
            });
        }
    }
}
