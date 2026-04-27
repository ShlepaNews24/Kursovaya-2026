using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RatingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ratings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rating>>> GetRatings()
        {
            return await _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Game)
                .ToListAsync();
        }

        // GET: api/ratings/game/5
        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<Rating>>> GetRatingsByGame(int gameId)
        {
            return await _context.Ratings
                .Where(r => r.GameId == gameId)
                .Include(r => r.User)
                .ToListAsync();
        }

        // GET: api/ratings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Rating>> GetRating(int id)
        {
            var rating = await _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Game)
                .FirstOrDefaultAsync(r => r.RatingId == id);

            if (rating == null)
            {
                return NotFound();
            }

            return rating;
        }

        // PUT: api/ratings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRating(int id, Rating rating)
        {
            if (id != rating.RatingId)
            {
                return BadRequest();
            }

            _context.Entry(rating).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RatingExists(id))
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

        // POST: api/ratings
        [HttpPost]
        public async Task<ActionResult<Rating>> PostRating(Rating rating)
        {
            // Проверяем, не оценивал ли уже пользователь эту игру
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == rating.UserId && r.GameId == rating.GameId);

            if (existingRating != null)
            {
                // Обновляем существующий рейтинг
                existingRating.RatingValue = rating.RatingValue;
                await _context.SaveChangesAsync();
                return Ok(existingRating);
            }

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRating), new { id = rating.RatingId }, rating);
        }

        // DELETE: api/ratings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRating(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null)
            {
                return NotFound();
            }

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RatingExists(int id)
        {
            return _context.Ratings.Any(e => e.RatingId == id);
        }
    }
}