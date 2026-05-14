using System.Net.Http.Json;
using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public class GenreService
    {
        private readonly HttpClient _http;
        public GenreService(HttpClient http) => _http = http;

        public async Task<List<GenreDto>?> GetGenresAsync()
        {
            try { return await _http.GetFromJsonAsync<List<GenreDto>>("api/genres"); }
            catch { return null; }
        }
    }
}