using GamesPlatform.API.Models;

namespace GamesPlatform.API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Game> Games { get; }
        IRepository<Genre> Genres { get; }
        IRepository<User> Users { get; }
        IRepository<Comment> Comments { get; }
        IRepository<Rating> Ratings { get; }
        Task<(List<GameDto> Items, int TotalCount)> GetPagedGamesAsync(int pageNumber, int pageSize, string? search, int? genreId);
        Task<int> SaveAsync();
    }
}