// [НАЗНАЧЕНИЕ] Реализация сервиса игр с поддержкой рейтингов
// [ФАЙЛ] Services/GamesService.cs

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

        public async Task<List<GameDto>?> GetGamesAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GameDto>>("api/games"); }
            catch (Exception ex) { _notify.Show($"Ошибка загрузки игр: {ex.Message}", "error"); return null; }
        }

        public async Task<GameDto?> GetGameAsync(int id)
        {
            try { return await _http.GetFromJsonAsync<GameDto>($"api/games/{id}"); }
            catch (Exception ex) { _notify.Show($"Ошибка загрузки игры: {ex.Message}", "error"); return null; }
        }

        public async Task<bool> CreateGameAsync(GameDto game)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(game.GameUrl))
                {
                    _notify.Show("URL игры обязателен", "error");
                    return false;
                }
                
                if (!Uri.IsWellFormedUriString(game.GameUrl, UriKind.Absolute))
                {
                    _notify.Show("Некорректный формат URL", "error");
                    return false;
                }
                
                var response = await _http.PostAsJsonAsync("api/games", game);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Не удалось создать игру: {error}", "error");
                    return false;
                }
                
                _notify.Show("Игра успешно создана!", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return false;
            }
        }

        public async Task<bool> UpdateGameAsync(int id, GameDto game)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/games/{id}", game);
                if (!response.IsSuccessStatusCode)
                {
                    _notify.Show("Не удалось обновить игру", "error");
                    return false;
                }
                _notify.Show("Игра обновлена!", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return false;
            }
        }

        public async Task<bool> DeleteGameAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/games/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    _notify.Show("Не удалось удалить игру", "error");
                    return false;
                }
                _notify.Show("Игра удалена", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return false;
            }
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
                if (!response.IsSuccessStatusCode)
                {
                    _notify.Show("Не удалось отправить комментарий", "error");
                    return false;
                }
                _notify.Show("Комментарий добавлен!", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return false;
            }
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Получение рейтинга игры
        // ====================================================================
        public async Task<RatingSummaryDto?> GetRatingAsync(int gameId)
        {
            try
            {
                return await _http.GetFromJsonAsync<RatingSummaryDto>($"api/ratings/game/{gameId}");
            }
            catch
            {
                return null;
            }
        }

        // ====================================================================
        // [НАЗНАЧЕНИЕ] Добавление или обновление оценки
        // ====================================================================
        public async Task<RatingSummaryDto?> AddRatingAsync(int gameId, int ratingValue)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/ratings", new { gameId, ratingValue });
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Не удалось поставить оценку: {error}", "error");
                    return null;
                }
                
                _notify.Show("Оценка сохранена!", "success");
                return await response.Content.ReadFromJsonAsync<RatingSummaryDto>();
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return null;
            }
        }
    }
}