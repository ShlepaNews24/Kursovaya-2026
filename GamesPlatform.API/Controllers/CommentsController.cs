// [НАЗНАЧЕНИЕ] Контроллер комментариев с загрузкой имени пользователя
// [ФАЙЛ] Controllers/CommentsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Получение комментариев для игры
        // [ИСПРАВЛЕНО] Добавлен .Include(c => c.User) для загрузки ника
        // ====================================================================
        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByGame(int gameId)
        {
            var comments = await _context.Comments
                .Include(c => c.User) // ✅ Загружаем связанного пользователя
                .Where(c => c.GameId == gameId)
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    CommentText = c.CommentText,
                    CreatedDate = c.CreatedDate,
                    UserId = c.UserId,
                    GameId = c.GameId,
                    UserName = c.User != null ? c.User.UserName : null // ✅ Заполняем UserName
                })
                .ToListAsync();

            return comments;
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Добавление комментария
        // ====================================================================
        [HttpPost]
        public async Task<ActionResult<CommentDto>> PostComment(Comment comment)
        {
            if (string.IsNullOrWhiteSpace(comment.CommentText))
                return BadRequest("Текст комментария не может быть пустым");

            // Проверка существования пользователя и игры
            var userExists = await _context.Users.AnyAsync(u => u.UserId == comment.UserId);
            if (!userExists) return BadRequest("Пользователь не найден");

            var gameExists = await _context.Games.AnyAsync(g => g.GameId == comment.GameId);
            if (!gameExists) return BadRequest("Игра не найдена");

            comment.CreatedDate = DateTime.UtcNow;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Возвращаем DTO с заполненным UserName
            var user = await _context.Users.FindAsync(comment.UserId);
            
            return CreatedAtAction(nameof(GetCommentsByGame), new { gameId = comment.GameId }, new CommentDto
            {
                CommentId = comment.CommentId,
                CommentText = comment.CommentText,
                CreatedDate = comment.CreatedDate,
                UserId = comment.UserId,
                GameId = comment.GameId,
                UserName = user?.UserName
            });
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Удаление комментария
        // ====================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] DTO для передачи комментария клиенту
    // ========================================================================
    public class CommentDto
    {
        public int CommentId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
        public int GameId { get; set; }
        public string? UserName { get; set; } // ✅ Поле для ника
    }
}