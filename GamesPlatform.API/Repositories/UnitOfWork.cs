using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;
using GamesPlatform.API.Repositories;

namespace GamesPlatform.API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IRepository<Game>? _games;
        private IRepository<Genre>? _genres;
        private IRepository<User>? _users;
        private IRepository<Comment>? _comments;
        private IRepository<Rating>? _ratings;

        public UnitOfWork(AppDbContext context) => _context = context;

        public IRepository<Game> Games => _games ??= new Repository<Game>(_context);
        public IRepository<Genre> Genres => _genres ??= new Repository<Genre>(_context);
        public IRepository<User> Users => _users ??= new Repository<User>(_context);
        public IRepository<Comment> Comments => _comments ??= new Repository<Comment>(_context);
        public IRepository<Rating> Ratings => _ratings ??= new Repository<Rating>(_context);

        public async Task<(List<GameDto> Items, int TotalCount)> GetPagedGamesAsync(int pageNumber, int pageSize, string? search, int? genreId)
        {
            var query = _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Developer)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(g => g.GameTitle.Contains(search));

            if (genreId.HasValue)
                query = query.Where(g => g.GenreId == genreId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(g => g.ModifiedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                    DeveloperId = g.DeveloperId,
                    GenreName = g.Genre != null ? g.Genre.GenreName : null,
                    DeveloperName = g.Developer != null ? g.Developer.UserName : null
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}