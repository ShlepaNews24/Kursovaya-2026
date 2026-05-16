using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByGame(int gameId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.GameId == gameId)
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    CommentText = c.CommentText,
                    CreatedDate = c.CreatedDate,
                    UserId = c.UserId,
                    GameId = c.GameId,
                    UserName = c.User != null ? c.User.UserName : null,
                    AvatarUrl = c.User != null ? c.User.AvatarUrl : null
                })
                .ToListAsync();
            return Ok(comments);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentDto>> PostComment(Comment comment)
        {
            if (string.IsNullOrWhiteSpace(comment.CommentText))
                return BadRequest("Текст комментария не может быть пустым");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            comment.UserId = userId;
            comment.CreatedDate = DateTime.UtcNow;

            var userExists = await _context.Users.AnyAsync(u => u.UserId == comment.UserId);
            if (!userExists) return BadRequest("Пользователь не найден");

            var gameExists = await _context.Games.AnyAsync(g => g.GameId == comment.GameId);
            if (!gameExists) return BadRequest("Игра не найдена");

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(comment.UserId);
            return CreatedAtAction(nameof(GetCommentsByGame), new { gameId = comment.GameId }, new CommentDto
            {
                CommentId = comment.CommentId,
                CommentText = comment.CommentText,
                CreatedDate = comment.CreatedDate,
                UserId = comment.UserId,
                GameId = comment.GameId,
                UserName = user?.UserName,
                AvatarUrl = user?.AvatarUrl
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userIdClaim == null) return Unauthorized();

            if (userRole != "Admin" && (!int.TryParse(userIdClaim.Value, out var userId) || comment.UserId != userId))
                return Forbid("Нет прав на удаление");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
        public int GameId { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}