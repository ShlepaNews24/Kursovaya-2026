// Контроллер управления играми
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging; // ✅ Для ILogger
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GamesController> _logger; // ✅ Добавлено логирование

        // ✅ Внедряем ILogger в конструктор
        public GamesController(IUnitOfWork uow, ILogger<GamesController> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<GameDto>>> GetGames([FromQuery] GamesQueryDto query)
        {
            _logger.LogInformation("📥 Request: GET /api/games (Page={Page}, Search={Search})", 
                query.PageNumber, query.Search ?? "null");

            var games = (await _uow.Games.GetAllAsync())
                .OrderByDescending(g => g.ModifiedDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                _logger.LogDebug("🔍 Filtering by search: {Search}", query.Search);
                games = games.Where(g => g.GameTitle.Contains(query.Search));
            }

            if (query.GenreId.HasValue)
            {
                _logger.LogDebug("🔍 Filtering by GenreId: {GenreId}", query.GenreId);
                games = games.Where(g => g.GenreId == query.GenreId.Value);
            }

            var totalCount = games.Count();

            var items = games
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(g => new GameDto
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
                })
                .ToList();

            _logger.LogInformation("✅ Returned {Count} games (Total: {Total})", items.Count, totalCount);
            return Ok(new PagedResult<GameDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameDto>> GetGame(int id)
        {
            _logger.LogDebug("🔍 Getting game with ID: {Id}", id);

            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) 
            {
                _logger.LogWarning("⚠️ Game not found: {Id}", id);
                return NotFound("Игра не найдена");
            }

            _logger.LogInformation("✅ Game retrieved: {Title}", game.GameTitle);
            return Ok(new GameDto
            {
                GameId = game.GameId,
                GameTitle = game.GameTitle,
                Description = game.Description,
                ReleaseDate = game.ReleaseDate,
                ModifiedDate = game.ModifiedDate,
                Logo = game.Logo,
                GameUrl = game.GameUrl,
                GenreId = game.GenreId,
                DeveloperId = game.DeveloperId
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GameDto>> PostGame(Game game)
        {
            _logger.LogInformation("🆕 Create request for game: {Title}", game.GameTitle);

            if (string.IsNullOrWhiteSpace(game.GameTitle))
            {
                _logger.LogWarning("⚠️ Validation failed: empty title");
                return BadRequest("Название игры обязательно");
            }
            
            if (string.IsNullOrWhiteSpace(game.GameUrl))
            {
                _logger.LogWarning("⚠️ Validation failed: empty URL");
                return BadRequest("URL игры обязателен");
            }
            
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
            {
                _logger.LogWarning("⚠️ Validation failed: invalid URL format");
                return BadRequest("Некорректный формат URL");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("⚠️ Authorization failed: cannot determine user");
                return Unauthorized("Не удалось определить пользователя");
            }

            game.DeveloperId = userId;
            game.ModifiedDate = DateTime.UtcNow;

            if (!await _uow.Genres.ExistsAsync(game.GenreId))
            {
                _logger.LogWarning("⚠️ Genre not found: {GenreId}", game.GenreId);
                return BadRequest("Указанный жанр не существует");
            }

            try
            {
                await _uow.Games.AddAsync(game);
                await _uow.SaveAsync();
                
                _logger.LogInformation("✅ Game created: {Title} by User {UserId}", 
                    game.GameTitle, game.DeveloperId);
                    
                return CreatedAtAction(nameof(GetGame), new { id = game.GameId }, new GameDto
                {
                    GameId = game.GameId,
                    GameTitle = game.GameTitle,
                    Description = game.Description,
                    ReleaseDate = game.ReleaseDate,
                    ModifiedDate = game.ModifiedDate,
                    Logo = game.Logo,
                    GameUrl = game.GameUrl,
                    GenreId = game.GenreId,
                    DeveloperId = game.DeveloperId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create game: {Title}", game.GameTitle);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            _logger.LogInformation("✏️ Update request for game ID: {Id}", id);

            if (id != game.GameId) 
            {
                _logger.LogWarning("⚠️ ID mismatch: URL={UrlId}, Body={BodyId}", id, game.GameId);
                return BadRequest("ID в URL и теле запроса не совпадают");
            }
            
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Некорректный формат URL");

            var existing = await _uow.Games.GetByIdAsync(id);
            if (existing == null) 
            {
                _logger.LogWarning("⚠️ Game not found for update: {Id}", id);
                return NotFound("Игра не найдена");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (userRole != "Admin" && existing.DeveloperId != userId)
            {
                _logger.LogWarning("⚠️ Forbidden: User {UserId} tried to edit game {GameId} owned by {OwnerId}", 
                    userId, id, existing.DeveloperId);
                return StatusCode(403, "Нет прав на редактирование чужой игры");
            }

            existing.GameTitle = game.GameTitle;
            existing.Description = game.Description;
            existing.GameUrl = game.GameUrl;
            existing.ReleaseDate = game.ReleaseDate;
            existing.GenreId = game.GenreId;
            existing.Logo = game.Logo;
            existing.ModifiedDate = DateTime.UtcNow;

            await _uow.Games.UpdateAsync(existing);
            await _uow.SaveAsync();

            _logger.LogInformation("✅ Game updated: {Title}", existing.GameTitle);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(int id)
        {
            _logger.LogInformation("🗑️ Delete request for game ID: {Id}", id);

            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) 
            {
                _logger.LogWarning("⚠️ Game not found for delete: {Id}", id);
                return NotFound("Игра не найдена");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (userRole != "Admin" && game.DeveloperId != userId)
            {
                _logger.LogWarning("⚠️ Forbidden: User {UserId} tried to delete game {GameId} owned by {OwnerId}", 
                    userId, id, game.DeveloperId);
                return StatusCode(403, "Нет прав на удаление чужой игры");
            }

            await _uow.Games.DeleteAsync(game);
            await _uow.SaveAsync();

            _logger.LogInformation("✅ Game deleted: {Title}", game.GameTitle);
            return NoContent();
        }
    }

    
}