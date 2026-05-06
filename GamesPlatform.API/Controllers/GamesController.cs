// Контроллер управления играми с пагинацией и фильтрацией
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GamesController(AppDbContext context) => _context = context;

        // (ПАГИНАЦИЯ И ФИЛЬТРАЦИЯ) Получение списка игр
        [HttpGet]
        public async Task<ActionResult<PagedResult<GameDto>>> GetGames([FromQuery] GamesQueryDto query)
        {
            var queryable = _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                queryable = queryable.Where(g => g.GameTitle.Contains(query.Search));
            }

            if (query.GenreId.HasValue)
            {
                queryable = queryable.Where(g => g.GenreId == query.GenreId.Value);
            }

            var totalCount = await queryable.CountAsync();

            var items = await queryable
                .OrderByDescending(g => g.ModifiedDate)
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
                    GenreName = g.Genre != null ? g.Genre.GenreName : null,
                    DeveloperId = g.DeveloperId,
                    DeveloperName = g.Developer != null ? g.Developer.UserName : null
                })
                .ToListAsync();

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
            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .FirstOrDefaultAsync(g => g.GameId == id);

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
                GenreName = game.Genre?.GenreName,
                DeveloperId = game.DeveloperId,
                DeveloperName = game.Developer?.UserName
            });
        }

        // Новая игра
        // Авторизованные пользователи
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

            var genreExists = await _context.Genres.AnyAsync(g => g.GenreId == game.GenreId);
            if (!genreExists) return BadRequest("Указанный жанр не существует");

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

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

        // Изменение игры
        // (ДОСТУП) Автор или Админ
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            if (id != game.GameId) return BadRequest("ID в URL и теле запроса не совпадают");
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Некорректный формат URL");

            var existing = await _context.Games.FindAsync(id);
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

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Удаление игры
        // (ДОСТУП) Автор или Админ
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound("Игра не найдена");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (userRole != "Admin" && game.DeveloperId != userId)
                return StatusCode(403, "Нет прав на удаление чужой игры");

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
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