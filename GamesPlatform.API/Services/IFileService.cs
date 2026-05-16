namespace GamesPlatform.API.Services
{
    public interface IFileService
    {
        Task<string?> UploadFileAsync(IFormFile file, string subFolder = "uploads");
        Task<bool> DeleteFileAsync(string filePath);
        bool IsValidFile(IFormFile file, out string errorMessage);
    }
}