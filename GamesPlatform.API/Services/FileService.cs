using Microsoft.AspNetCore.Http;

namespace GamesPlatform.API.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly int _maxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsValidFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "Файл не выбран";
                return false;
            }

            if (file.Length > _maxFileSize)
            {
                errorMessage = $"Размер файла не должен превышать {_maxFileSize / (1024 * 1024)} МБ";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                errorMessage = $"Допустимые форматы: {string.Join(", ", _allowedExtensions)}";
                return false;
            }

            return true;
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string subFolder = "uploads")
        {
            if (!IsValidFile(file, out var errorMessage))
            {
                _logger.LogWarning("File upload validation failed: {ErrorMessage}", errorMessage);
                return null;
            }

            try
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, subFolder);
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null)
                {
                    _logger.LogWarning("HttpContext is null, cannot build full URL");
                    return null;
                }

                var relativePath = $"/{subFolder}/{uniqueFileName}";
                var fullUrl = $"{request.Scheme}://{request.Host}{relativePath}";
                _logger.LogInformation("File uploaded: {FullUrl}", fullUrl);
                return fullUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file");
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                var uri = new Uri(filePath);
                var relativePath = uri.PathAndQuery;
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted: {FullPath}", fullPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }
    }
}