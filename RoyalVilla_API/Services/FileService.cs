namespace RoyalVilla_API.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileService> _logger;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public FileService(IWebHostEnvironment webHostEnvironment, ILogger<FileService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public bool ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            // Check file size
            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning($"File size {file.Length} exceeds maximum allowed size {MaxFileSize}");
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"File extension {extension} is not allowed");
                return false;
            }

            return true;
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            try
            {
                if (!ValidateImage(file))
                {
                    throw new InvalidOperationException("Invalid image file");
                }

                // Create images directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "villas");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique file name
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative URL
                var imageUrl = $"/images/villas/{uniqueFileName}";
                _logger.LogInformation($"Image uploaded successfully: {imageUrl}");
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return false;
                }

                // Extract file name from URL
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "villas", fileName);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation($"Image deleted successfully: {imageUrl}");
                    return true;
                }

                _logger.LogWarning($"Image file not found: {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image: {imageUrl}");
                return false;
            }
        }
    }
}
