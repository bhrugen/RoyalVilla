namespace RoyalVilla_API.Services.IServices
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string imageUrl);
        bool ValidateImage(IFormFile file);
    }
}
