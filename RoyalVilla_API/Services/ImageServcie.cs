using RoyalVilla_API.Services.IServices;

namespace RoyalVilla_API.Services
{
    public class ImageServcie : IImageService
    {

        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png"};
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ImageServcie(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment=webHostEnvironment;
        }
        public Task<bool> DeleteImageAsync(string imageUrl)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadImageAsync(IFormFile file)
        {
            throw new NotImplementedException();
        }

        public bool ValidateImage(IFormFile file)
        {
            throw new NotImplementedException();
        }
    }
}
