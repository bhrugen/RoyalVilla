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

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            try
            {
                if (!ValidateImage(file))
                {
                    throw new InvalidOperationException("Invalid image file");
                }

                var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "villas");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                //save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/images/villas/{uniqueFileName}";

            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool ValidateImage(IFormFile file)
        {
            if(file==null  || file.Length == 0)
            {
                return false;
            }
            if (file.Length > MaxFileSize)
            {
                return false;
            }
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return false;
            }
            return true;
        }
    }
}
