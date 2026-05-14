using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;
        private const string GenresCacheKey = "all_genres";

        public GenresController(IUnitOfWork uow, IMemoryCache cache)
        {
            _uow = uow;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenreDto>>> GetGenres()
        {
            if (!_cache.TryGetValue(GenresCacheKey, out List<GenreDto>? genres))
            {
                var genreEntities = await _uow.Genres.GetAllAsync();
                genres = genreEntities.Select(g => new GenreDto { GenreId = g.GenreId, GenreName = g.GenreName }).ToList();
                _cache.Set(GenresCacheKey, genres, TimeSpan.FromHours(1));
            }
            return Ok(genres);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GenreDto>> GetGenre(int id)
        {
            var genre = await _uow.Genres.GetByIdAsync(id);
            if (genre == null) return NotFound();
            return Ok(new GenreDto { GenreId = genre.GenreId, GenreName = genre.GenreName });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<GenreDto>> PostGenre(Genre genre)
        {
            await _uow.Genres.AddAsync(genre);
            await _uow.SaveAsync();
            _cache.Remove(GenresCacheKey);
            return CreatedAtAction(nameof(GetGenre), new { id = genre.GenreId }, new GenreDto { GenreId = genre.GenreId, GenreName = genre.GenreName });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGenre(int id, Genre genre)
        {
            if (id != genre.GenreId) return BadRequest();
            await _uow.Genres.UpdateAsync(genre);
            await _uow.SaveAsync();
            _cache.Remove(GenresCacheKey);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _uow.Genres.GetByIdAsync(id);
            if (genre == null) return NotFound();
            await _uow.Genres.DeleteAsync(genre);
            await _uow.SaveAsync();
            _cache.Remove(GenresCacheKey);
            return NoContent();
        }
    }

    public class GenreDto
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }
}