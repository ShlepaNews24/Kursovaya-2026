using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Все операции с файлами требуют авторизации
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FilesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Загрузка файла игры: POST /api/files/games/{gameId}
        [HttpPost("games/{gameId}")]
        public async Task<IActionResult> UploadGameFile(int gameId, IFormFile file)
        {
            // Проверка существования игры
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound("Игра не найдена");

            // Валидация: файл не должен быть пустым
            if (file == null || file.Length == 0)
                return BadRequest("Файл пуст");

            // Валидация размера (макс. 50 МБ)
            const long maxFileSize = 50 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest("Размер файла превышает 50 МБ");

            // Валидация расширения (только zip, html, js)
            var allowedExtensions = new[] { ".zip", ".html", ".js" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest("Недопустимый формат файла. Разрешены: .zip, .html, .js");

            // Формирование безопасного пути сохранения
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "games", gameId.ToString());
            Directory.CreateDirectory(uploadPath);
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Сохранение файла на диск
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Обновление записи в БД
            game.FilePath = $"/uploads/games/{gameId}/{uniqueFileName}";
            game.FileSize = file.Length;
            game.ModifiedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Файл успешно загружен", path = game.FilePath });
        }

        // Скачивание файла игры: GET /api/files/games/{gameId}/download
        [HttpGet("games/{gameId}/download")]
        public async Task<IActionResult> DownloadGameFile(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null || string.IsNullOrEmpty(game.FilePath))
                return NotFound("Файл не найден");

            var fullPath = Path.Combine(_env.WebRootPath, game.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                return NotFound("Файл отсутствует на сервере");

            // Возврат файла с правильным MIME-типом
            var memory = new MemoryStream();
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            await stream.CopyToAsync(memory);
            memory.Position = 0;

            return File(memory, "application/octet-stream", Path.GetFileName(fullPath));
        }
    }
}