using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;

        public RatingsController(IUnitOfWork uow, IMemoryCache cache)
        {
            _uow = uow;
            _cache = cache;
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<RatingSummaryDto>> GetGameRating(int gameId)
        {
            var cacheKey = $"rating_{gameId}";
            if (!_cache.TryGetValue(cacheKey, out RatingSummaryDto? summary))
            {
                var gameExists = await _uow.Games.ExistsAsync(gameId);
                if (!gameExists) return NotFound("Игра не найдена");

                var ratings = (await _uow.Ratings.FindAsync(r => r.GameId == gameId)).ToList();
                double average = ratings.Any() ? ratings.Average(r => r.RatingValue) : 0;
                int count = ratings.Count;

                int? userRating = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    userRating = ratings.FirstOrDefault(r => r.UserId == userId)?.RatingValue;
                }

                summary = new RatingSummaryDto
                {
                    GameId = gameId,
                    AverageRating = Math.Round(average, 1),
                    TotalVotes = count,
                    UserRating = userRating
                };
                _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(1));
            }
            return Ok(summary);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<RatingSummaryDto>> PostRating([FromBody] RatingDto dto)
        {
            if (dto.RatingValue < 1 || dto.RatingValue > 5)
                return BadRequest("Оценка должна быть от 1 до 5");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Не удалось определить пользователя");

            var gameExists = await _uow.Games.ExistsAsync(dto.GameId);
            if (!gameExists) return BadRequest("Игра не найдена");

            var existing = (await _uow.Ratings.FindAsync(r => r.GameId == dto.GameId && r.UserId == userId)).FirstOrDefault();

            if (existing != null)
            {
                existing.RatingValue = dto.RatingValue;
                await _uow.Ratings.UpdateAsync(existing);
            }
            else
            {
                await _uow.Ratings.AddAsync(new Rating
                {
                    GameId = dto.GameId,
                    UserId = userId,
                    RatingValue = dto.RatingValue
                });
            }

            await _uow.SaveAsync();

            _cache.Remove($"rating_{dto.GameId}");

            var ratings = (await _uow.Ratings.FindAsync(r => r.GameId == dto.GameId)).ToList();
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

    public class RatingSummaryDto
    {
        public int GameId { get; set; }
        public double AverageRating { get; set; }
        public int TotalVotes { get; set; }
        public int? UserRating { get; set; }
    }

    public class RatingDto
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }

        [JsonPropertyName("ratingValue")]
        public int RatingValue { get; set; }
    }
}