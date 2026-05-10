using System.Net.Http;
using System.Net.Http.Json;
using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public class GamesService : IGamesService
    {
        private readonly HttpClient _http;
        private readonly INotificationService _notify;

        public GamesService(HttpClient http, INotificationService notify)
        {
            _http = http;
            _notify = notify;
        }

        public async Task<PagedResult<GameDto>> GetGamesAsync(int pageNumber = 1, int pageSize = 10, string? search = null, int? genreId = null)
        {
            try
            {
                var url = $"api/games?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={Uri.EscapeDataString(search)}";
                if (genreId.HasValue)
                    url += $"&genreId={genreId.Value}";

                var result = await _http.GetFromJsonAsync<PagedResult<GameDto>>(url);
                return result ?? new PagedResult<GameDto>();
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка загрузки игр: {ex.Message}", "error");
                return new PagedResult<GameDto>();
            }
        }

        public async Task<GameDto?> GetGameAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GameDto>($"api/games/{id}"); }
            catch { return null; }
        }

        public async Task<bool> AddGameAsync(GameDto game)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/games", game);
                if (!response.IsSuccessStatusCode) { _notify.Show("Ошибка добавления игры", "error"); return false; }
                _notify.Show("Игра успешно добавлена!", "success");
                return true;
            }
            catch (Exception ex) { _notify.Show($"Ошибка сети: {ex.Message}", "error"); return false; }
        }

        public async Task<bool> UpdateGameAsync(GameDto game)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/games/{game.GameId}", game);
                if (!response.IsSuccessStatusCode) { _notify.Show("Ошибка обновления", "error"); return false; }
                _notify.Show("Игра обновлена!", "success");
                return true;
            }
            catch (Exception ex) { _notify.Show($"Ошибка сети: {ex.Message}", "error"); return false; }
        }

        public async Task<bool> DeleteGameAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/games/{id}");
                if (!response.IsSuccessStatusCode) { _notify.Show("Ошибка удаления", "error"); return false; }
                _notify.Show("Игра удалена", "success");
                return true;
            }
            catch (Exception ex) { _notify.Show($"Ошибка сети: {ex.Message}", "error"); return false; }
        }

        public async Task<List<CommentDto>?> GetCommentsAsync(int gameId)
        {
            try { return await _http.GetFromJsonAsync<List<CommentDto>>($"api/comments/game/{gameId}"); }
            catch { return null; }
        }

        public async Task<bool> AddCommentAsync(CommentDto comment)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/comments", comment);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/comments/{commentId}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<RatingSummaryDto?> GetRatingAsync(int gameId)
        {
            try { return await _http.GetFromJsonAsync<RatingSummaryDto>($"api/ratings/{gameId}"); }
            catch { return null; }
        }

        public async Task<bool> AddRatingAsync(int gameId, int ratingValue)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/ratings", new { gameId, ratingValue });
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<GenreDto>?> GetGenresAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GenreDto>>("api/genres"); }
            catch { return null; }
        }
    }
}