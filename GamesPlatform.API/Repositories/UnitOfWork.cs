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

        public UnitOfWork(AppDbContext context) => _context = context;

        public IRepository<Game> Games => _games ??= new Repository<Game>(_context);
        public IRepository<Genre> Genres => _genres ??= new Repository<Genre>(_context);
        public IRepository<User> Users => _users ??= new Repository<User>(_context);

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();
    }
}