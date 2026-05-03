// [НАЗНАЧЕНИЕ] Интерфейс сервиса игр
// [ФАЙЛ] Services/IGamesService.cs

using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public interface IGamesService
    {
        Task<List<GameDto>?> GetGamesAsync();
        Task<GameDto?> GetGameAsync(int id);
        Task<bool> CreateGameAsync(GameDto game);
        Task<bool> UpdateGameAsync(int id, GameDto game);
        Task<bool> DeleteGameAsync(int id);
        
        Task<List<CommentDto>?> GetCommentsAsync(int gameId);
        Task<bool> AddCommentAsync(CommentDto comment);
        
        // ✅ Методы для работы с рейтингом
        Task<RatingSummaryDto?> GetRatingAsync(int gameId);
        Task<RatingSummaryDto?> AddRatingAsync(int gameId, int ratingValue);
    }
}