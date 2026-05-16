using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public interface IGamesService
    {
        Task<PagedResult<GameDto>> GetGamesAsync(int pageNumber = 1, int pageSize = 10, string? search = null, int? genreId = null);
        Task<GameDto?> GetGameAsync(int id);
        Task<bool> AddGameAsync(GameDto game);
        Task<bool> UpdateGameAsync(GameDto game);
        Task<bool> DeleteGameAsync(int id);
        Task<List<CommentDto>?> GetCommentsAsync(int gameId);
        Task<bool> AddCommentAsync(CommentDto comment);
        Task<bool> DeleteCommentAsync(int commentId);
        Task<RatingSummaryDto?> GetRatingAsync(int gameId);
        Task<bool> AddRatingAsync(int gameId, int ratingValue);
        Task<List<GenreDto>?> GetGenresAsync();
        Task<string?> UploadLogoAsync(Stream fileStream, string fileName);
    }
}