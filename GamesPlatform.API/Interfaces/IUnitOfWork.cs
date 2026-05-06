using GamesPlatform.API.Models; 

namespace GamesPlatform.API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Game> Games { get; }  
        IRepository<Comment> Comments { get; }
        IRepository<Genre> Genres { get; }
        IRepository<Rating> Ratings { get; }
        
        Task<int> SaveAsync();
    }
}