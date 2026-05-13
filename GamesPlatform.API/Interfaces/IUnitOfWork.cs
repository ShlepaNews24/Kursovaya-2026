using GamesPlatform.API.Models;

namespace GamesPlatform.API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Game> Games { get; }
        IRepository<Genre> Genres { get; }
        IRepository<User> Users { get; }
        Task<int> SaveAsync();
    }
}