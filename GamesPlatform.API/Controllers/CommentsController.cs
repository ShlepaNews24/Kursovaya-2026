using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Microsoft.AspNetCore.Authorization; 

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Game)
                .ToListAsync();
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByGame(int gameId)
        {
            return await _context.Comments
                .Where(c => c.GameId == gameId)
                .Include(c => c.User)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Game)
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            return comment;
        }

        // Оставить комментарий к игре
        // Только авторизованные 
        [HttpPost]
        [Authorize]  // ← Защита: комментировать могут только вошедшие
        public async Task<ActionResult<Comment>> PostComment(Comment comment)
        {
            comment.CreatedDate = DateTime.UtcNow;
            
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComment), new { id = comment.CommentId }, comment);
        }

        // Редактировать свой комментарий
        [HttpPut("{id}")]
        [Authorize]  // ← Защита от редактирования чужих комментариев
        public async Task<IActionResult> PutComment(int id, Comment comment)
        {
            if (id != comment.CommentId)
            {
                return BadRequest();
            }

            _context.Entry(comment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
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

        // Удалить комментарий (модерация)
        // Только администраторы — обычные пользователи удаляют только свои
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]  // ← Удаление — привилегия админа
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.CommentId == id);
        }
    }
}