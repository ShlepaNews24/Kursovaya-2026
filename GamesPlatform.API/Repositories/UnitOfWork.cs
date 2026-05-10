using GamesPlatform.API.Data;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models; 

namespace GamesPlatform.API.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IRepository<User> Users { get; private set; }
        public IRepository<Game> Games { get; private set; }  
        public IRepository<Comment> Comments { get; private set; }
        public IRepository<Genre> Genres { get; private set; }
        public IRepository<Rating> Ratings { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new Repository<User>(context);
            Games = new Repository<Game>(context); 
            Comments = new Repository<Comment>(context);
            Genres = new Repository<Genre>(context);
            Ratings = new Repository<Rating>(context);
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
        
        public void Dispose() => _context.Dispose();
    }
}