// [НАЗНАЧЕНИЕ] Контроллер для работы с оценками игр
// [ФАЙЛ] Controllers/RatingsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RatingsController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Получение сводки рейтинга для игры
        // [МАРШРУТ] GET /api/ratings/game/{gameId}
        // [ВОЗВРАЩАЕТ] Средний рейтинг, количество голосов и оценку текущего пользователя
        // ====================================================================
        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<RatingSummaryDto>> GetGameRating(int gameId)
        {
            var gameExists = await _context.Games.AnyAsync(g => g.GameId == gameId);
            if (!gameExists) return NotFound("Игра не найдена");

            // Подсчёт среднего и количества
            var ratings = await _context.Ratings
                .Where(r => r.GameId == gameId)
                .ToListAsync();

            double average = ratings.Any() ? ratings.Average(r => r.RatingValue) : 0;
            int count = ratings.Count;

            // Оценка текущего пользователя (если авторизован)
            int? userRating = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                userRating = ratings.FirstOrDefault(r => r.UserId == userId)?.RatingValue;
            }

            return new RatingSummaryDto
            {
                GameId = gameId,
                AverageRating = Math.Round(average, 1),
                TotalVotes = count,
                UserRating = userRating
            };
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Добавление или обновление оценки
        // [МАРШРУТ] POST /api/ratings
        // [ДОСТУП] Только авторизованные пользователи
        // [ОСОБЕННОСТЬ] Если оценка уже есть — обновляет её
        // ====================================================================
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<RatingSummaryDto>> PostRating(RatingDto dto)
        {
            if (dto.RatingValue < 1 || dto.RatingValue > 5)
                return BadRequest("Оценка должна быть от 1 до 5");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Не удалось определить пользователя");

            var gameExists = await _context.Games.AnyAsync(g => g.GameId == dto.GameId);
            if (!gameExists) return BadRequest("Игра не найдена");

            // Проверка существующей оценки
            var existing = await _context.Ratings
                .FirstOrDefaultAsync(r => r.GameId == dto.GameId && r.UserId == userId);

            if (existing != null)
            {
                // Обновляем оценку
                existing.RatingValue = dto.RatingValue;
            }
            else
            {
                // Создаём новую
                _context.Ratings.Add(new Rating
                {
                    GameId = dto.GameId,
                    UserId = userId,
                    RatingValue = dto.RatingValue
                });
            }

            await _context.SaveChangesAsync();

            // Возвращаем обновлённую сводку
            var ratings = await _context.Ratings.Where(r => r.GameId == dto.GameId).ToListAsync();
            double average = ratings.Any() ? ratings.Average(r => r.RatingValue) : 0;

            return Ok(new RatingSummaryDto
            {
                GameId = dto.GameId,
                AverageRating = Math.Round(average, 1),
                TotalVotes = ratings.Count,
                UserRating = dto.RatingValue
            });
        }
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] DTO для передачи сводки рейтинга
    // ========================================================================
    public class RatingSummaryDto
    {
        public int GameId { get; set; }
        public double AverageRating { get; set; }
        public int TotalVotes { get; set; }
        public int? UserRating { get; set; }
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] DTO для отправки оценки
    // ========================================================================
    public class RatingDto
    {
        public int GameId { get; set; }
        public int RatingValue { get; set; }
    }
}