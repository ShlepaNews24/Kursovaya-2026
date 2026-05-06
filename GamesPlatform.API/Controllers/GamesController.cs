// Контроллер управления играми 
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public GamesController(IUnitOfWork uow) => _uow = uow;

        [HttpGet]
        public async Task<ActionResult<PagedResult<GameDto>>> GetGames([FromQuery] GamesQueryDto query)
        {
            // Получаем все игры через репозиторий
            var games = (await _uow.Games.GetAllAsync())
                .OrderByDescending(g => g.ModifiedDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                games = games.Where(g => g.GameTitle.Contains(query.Search));
            }

            if (query.GenreId.HasValue)
            {
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
            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) return NotFound("Игра не найдена");

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
            if (string.IsNullOrWhiteSpace(game.GameTitle))
                return BadRequest("Название игры обязательно");
            
            if (string.IsNullOrWhiteSpace(game.GameUrl))
                return BadRequest("URL игры обязателен");
            
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Некорректный формат URL");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Не удалось определить пользователя");

            game.DeveloperId = userId;
            game.ModifiedDate = DateTime.UtcNow;

            if (!await _uow.Genres.ExistsAsync(game.GenreId))
                return BadRequest("Указанный жанр не существует");

            await _uow.Games.AddAsync(game);
            await _uow.SaveAsync(); 

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

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            if (id != game.GameId) return BadRequest("ID в URL и теле запроса не совпадают");
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Некорректный формат URL");

            var existing = await _uow.Games.GetByIdAsync(id);
            if (existing == null) return NotFound("Игра не найдена");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (userRole != "Admin" && existing.DeveloperId != userId)
                return StatusCode(403, "Нет прав на редактирование чужой игры");

            existing.GameTitle = game.GameTitle;
            existing.Description = game.Description;
            existing.GameUrl = game.GameUrl;
            existing.ReleaseDate = game.ReleaseDate;
            existing.GenreId = game.GenreId;
            existing.Logo = game.Logo;
            existing.ModifiedDate = DateTime.UtcNow;

            await _uow.Games.UpdateAsync(existing);
            await _uow.SaveAsync(); 

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _uow.Games.GetByIdAsync(id);
            if (game == null) return NotFound("Игра не найдена");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (userRole != "Admin" && game.DeveloperId != userId)
                return StatusCode(403, "Нет прав на удаление чужой игры");

            await _uow.Games.DeleteAsync(game);
            await _uow.SaveAsync(); 

            return NoContent();
        }
    }

    public class GameDto
    {
        public int GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? Logo { get; set; }
        public int GenreId { get; set; }
        public int DeveloperId { get; set; }
        public string GameUrl { get; set; } = string.Empty;
        public string? GenreName { get; set; }
        public string? DeveloperName { get; set; }
    }

    public class GamesQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? GenreId { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}