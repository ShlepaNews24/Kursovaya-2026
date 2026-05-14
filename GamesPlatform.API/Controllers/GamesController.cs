using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GamesController> _logger;
        private readonly IMemoryCache _cache;
        private const string GamesCachePrefix = "games_paged_";

        public GamesController(IUnitOfWork uow, ILogger<GamesController> logger, IMemoryCache cache)
        {
            _uow = uow;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<GameDto>>> GetGames([FromQuery] GamesQueryDto query)
        {
            var cacheKey = $"{GamesCachePrefix}{query.PageNumber}_{query.PageSize}_{query.Search}_{query.GenreId}";

            if (!_cache.TryGetValue(cacheKey, out PagedResult<GameDto>? result))
            {
                var (items, totalCount) = await _uow.GetPagedGamesAsync(
                    query.PageNumber, query.PageSize, query.Search, query.GenreId);

                result = new PagedResult<GameDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                _logger.LogDebug("Cache MISS for {CacheKey}", cacheKey);
            }
            else
            {
                _logger.LogDebug("Cache HIT for {CacheKey}", cacheKey);
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameDto>> GetGame(int id)
        {
            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) return NotFound("Игра не найдена");
            return Ok(MapToDto(game));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GameDto>> PostGame(CreateGameDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.GameTitle))
                return BadRequest("Название игры обязательно");
            if (string.IsNullOrWhiteSpace(dto.GameUrl) || !Uri.IsWellFormedUriString(dto.GameUrl, UriKind.Absolute))
                return BadRequest("Некорректный URL");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var game = new Game
            {
                GameTitle = dto.GameTitle,
                Description = dto.Description,
                ReleaseDate = dto.ReleaseDate,
                GameUrl = dto.GameUrl,
                Logo = dto.Logo,
                GenreId = dto.GenreId,
                DeveloperId = userId,
                ModifiedDate = DateTime.UtcNow
            };

            if (!await _uow.Genres.ExistsAsync(game.GenreId))
                return BadRequest("Жанр не существует");

            await _uow.Games.AddAsync(game);
            await _uow.SaveAsync();

            _cache.Remove(GamesCachePrefix);
            return CreatedAtAction(nameof(GetGame), new { id = game.GameId }, MapToDto(game));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutGame(int id, UpdateGameDto dto)
        {
            if (id != dto.GameId) return BadRequest("ID не совпадают");

            var existing = await _uow.Games.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && existing.DeveloperId != userId)
                return StatusCode(403, "Нет прав");

            existing.GameTitle = dto.GameTitle;
            existing.Description = dto.Description;
            existing.GameUrl = dto.GameUrl;
            existing.ReleaseDate = dto.ReleaseDate;
            existing.GenreId = dto.GenreId;
            existing.Logo = dto.Logo;
            existing.ModifiedDate = DateTime.UtcNow;

            await _uow.Games.UpdateAsync(existing);
            await _uow.SaveAsync();

            _cache.Remove(GamesCachePrefix);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && game.DeveloperId != userId)
                return StatusCode(403, "Нет прав");

            await _uow.Games.DeleteAsync(game);
            await _uow.SaveAsync();

            _cache.Remove(GamesCachePrefix);
            return NoContent();
        }

        private static GameDto MapToDto(Game g) => new()
        {
            GameId = g.GameId,
            GameTitle = g.GameTitle,
            Description = g.Description,
            ReleaseDate = g.ReleaseDate,
            ModifiedDate = g.ModifiedDate,
            Logo = g.Logo,
            GameUrl = g.GameUrl,
            GenreId = g.GenreId,
            DeveloperId = g.DeveloperId
        };
    }
}