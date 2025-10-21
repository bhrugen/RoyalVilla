namespace RoyalVilla_API.Services
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string imageUrl);
        bool ValidateImage(IFormFile file);
    }
}
