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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Game>>> GetGames()
        {
            return await _context.Games.Include(g => g.Genre).Include(g => g.Developer).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Games.Include(g => g.Genre).Include(g => g.Developer).FirstOrDefaultAsync(g => g.GameId == id);
            if (game == null) return NotFound();
            return game;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Game>> PostGame(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.GameTitle)) return BadRequest("Название обязательно");
            if (string.IsNullOrWhiteSpace(game.GameUrl)) return BadRequest("URL обязателен");
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute)) return BadRequest("Неверный URL");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            
            game.DeveloperId = userId;
            
            var genreExists = await _context.Genres.AnyAsync(g => g.GenreId == game.GenreId);
            if (!genreExists) return BadRequest("Жанр не найден");
            
            game.ModifiedDate = DateTime.UtcNow;
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGame), new { id = game.GameId }, game);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            if (id != game.GameId) return BadRequest();
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute)) return BadRequest("Неверный URL");

            var existing = await _context.Games.FindAsync(id);
            if (existing == null) return NotFound();
            
            // Проверка прав: автор или админ
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userIdClaim == null || (!int.TryParse(userIdClaim.Value, out var userId) && userRole != "Admin"))
                return Unauthorized();
            
            if (existing.DeveloperId != userId && userRole != "Admin")
                return Forbid("Нет прав на редактирование");

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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userIdClaim == null) return Unauthorized();
            
            if (userRole != "Admin" && (!int.TryParse(userIdClaim.Value, out var userId) || game.DeveloperId != userId))
                return Forbid("Нет прав на удаление");
            
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}