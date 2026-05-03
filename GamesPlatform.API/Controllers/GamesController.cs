// [ФАЙЛ] GamesPlatform.API/Controllers/GamesController.cs

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
            return await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .FirstOrDefaultAsync(g => g.GameId == id);
            
            if (game == null) return NotFound();
            return game;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Game>> PostGame(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.GameTitle))
                return BadRequest("Game title is required");
            
            if (string.IsNullOrWhiteSpace(game.GameUrl))
                return BadRequest("Game URL is required");
            
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Invalid URL format");
            
            // Безопасность: DeveloperId из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid token");
            
            game.DeveloperId = userId;
            
            var genreExists = await _context.Genres.AnyAsync(g => g.GenreId == game.GenreId);
            if (!genreExists) return BadRequest("Genre not found");
            
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
            
            if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                return BadRequest("Invalid URL format");
            
            var existing = await _context.Games.FindAsync(id);
            if (existing == null) return NotFound();
            
            existing.GameTitle = game.GameTitle;
            existing.Description = game.Description; // ✅ Обновляем описание
            existing.GameUrl = game.GameUrl;
            existing.ReleaseDate = game.ReleaseDate;
            existing.GenreId = game.GenreId;
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
            
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}