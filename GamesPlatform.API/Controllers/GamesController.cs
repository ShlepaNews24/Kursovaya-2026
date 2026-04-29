using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;  
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GamesController(AppDbContext context)
        {
            _context = context;
        }

        // Получить список всех игр (каталог)
        // (ДОСТУП) Публичный — любой пользователь может просматривать игры
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Game>>> GetGames()
        {
            return await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .ToListAsync();
        }

        // Получить игру по ID (страница игры)
        // (ДОСТУП) Публичный — любой пользователь может смотреть детали
        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .Include(g => g.Comments)
                .Include(g => g.Ratings)
                .FirstOrDefaultAsync(g => g.GameId == id);

            if (game == null)
            {
                return NotFound();
            }

            return game;
        }

        // Создать новую игру (загрузить свою игру)
        // Только авторизованные пользователи 
        // 
        [HttpPost]
        [Authorize]  // ←  Защита: только вошедшие пользователи
        public async Task<ActionResult<Game>> PostGame(Game game)
        {
            game.ModifiedDate = DateTime.UtcNow;
            
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGame), new { id = game.GameId }, game);
        }

        // Обновить существующую игру
        // Только авторизованные — редактировать могут только создатели
        [HttpPut("{id}")]
        [Authorize]  // ← Защита от несанкционированного редактирования
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            if (id != game.GameId)
            {
                return BadRequest();
            }

            game.ModifiedDate = DateTime.UtcNow;

            _context.Entry(game).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // Удалить игру (модерация)
        // Только администраторы — обычные пользователи не могут удалять
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]  // ← Строгая защита: только админы
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Вспомогательный метод проверки существования игры
        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.GameId == id);
        }
    }
}