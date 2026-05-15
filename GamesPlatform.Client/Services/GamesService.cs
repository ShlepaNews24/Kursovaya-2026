// Сервис для работы с играми, комментариями и рейтингами
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public class GamesService : IGamesService
    {
        private readonly HttpClient _http;
        private readonly INotificationService _notify;
        private readonly IAuthService _authService;

        public GamesService(HttpClient http, INotificationService notify, IAuthService authService)
        {
            _http = http;
            _notify = notify;
            _authService = authService;
        }

       
        // Получение списка игр с фильтрами
        
        public async Task<PagedResult<GameDto>> GetGamesAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? search = null, 
            int? genreId = null)
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
            try 
            { 
                return await _http.GetFromJsonAsync<GameDto>($"api/games/{id}"); 
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка загрузки игры: {ex.Message}", "error");
                return null; 
            }
        }

        // Добавление новой игры (с токеном)
        public async Task<bool> AddGameAsync(GameDto game)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/games");
                request.Content = JsonContent.Create(game);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) 
                { 
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Ошибка: {error}", "error"); 
                    return false; 
                }
                
                _notify.Show("Игра успешно добавлена!", "success");
                return true;
            }
            catch (Exception ex) 
            { 
                _notify.Show($"Ошибка сети: {ex.Message}", "error"); 
                return false; 
            }
        }


        // Обновление существующей игры

        public async Task<bool> UpdateGameAsync(GameDto game)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/games/{game.GameId}");
                request.Content = JsonContent.Create(game);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) 
                { 
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Ошибка обновления: {error}", "error"); 
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


        // Удаление игры

        public async Task<bool> DeleteGameAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/games/{id}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) 
                { 
                    _notify.Show("Ошибка удаления игры", "error"); 
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


        // Получение комментариев для игры

        public async Task<List<CommentDto>?> GetCommentsAsync(int gameId)
        {
            try 
            { 
                return await _http.GetFromJsonAsync<List<CommentDto>>($"api/comments/game/{gameId}"); 
            }
            catch 
            { 
                return null; 
            }
        }


        // Добавление комментария

        public async Task<bool> AddCommentAsync(CommentDto comment)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/comments");
                request.Content = JsonContent.Create(comment);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch 
            { 
                return false; 
            }
        }


        // Удаление комментария

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/comments/{commentId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch 
            { 
                return false; 
            }
        }

        // Получение сводки рейтинга игры

        public async Task<RatingSummaryDto?> GetRatingAsync(int gameId)
        {
            try 
            { 
                return await _http.GetFromJsonAsync<RatingSummaryDto>($"api/ratings/game/{gameId}"); 
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка загрузки рейтинга: {ex.Message}", "error");
                return null; 
            }
        }

        // Отправка оценки за игру
        public async Task<bool> AddRatingAsync(int gameId, int ratingValue)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _notify.Show("Ошибка: вы не авторизованы", "error");
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/ratings");
                request.Content = JsonContent.Create(new { gameId = gameId, ratingValue = ratingValue });
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Не удалось отправить оценку: {(int)response.StatusCode} {error}", "error");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Ошибка сети: {ex.Message}", "error");
                return false;
            }
        }


        // Получение списка жанров

        public async Task<List<GenreDto>?> GetGenresAsync()
        {
            try 
            { 
                return await _http.GetFromJsonAsync<List<GenreDto>>("api/genres"); 
            }
            catch 
            { 
                return null; 
            }
        }
    }
}