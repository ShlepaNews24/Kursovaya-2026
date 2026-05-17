using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public interface IAuthService
    {
        // Базовая аутентификация (возвращает успех и сообщение об ошибке)
        Task<(bool Success, string ErrorMessage)> LoginAsync(string email, string password);
        Task<(bool Success, string ErrorMessage)> RegisterAsync(string userName, string email, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();

        // Информация о пользователе
        Task<string?> GetUserRoleAsync();
        Task<string?> GetUserIdAsync();
        Task<string?> GetUserNameAsync();
        Task<string?> GetUserEmailAsync();
        Task<DateTime?> GetRegistrationDateAsync();
        Task<DateTime?> GetLastLoginDateAsync();
        Task<DateTime?> GetDateOfBirthAsync();

        // Аватар
        Task<string?> GetAvatarUrlAsync();
        Task<bool> UploadAvatarAsync(Stream fileStream, string fileName);
        Task<bool> DeleteAvatarAsync();

        // Обновление профиля
        Task<bool> UpdateUserNameAsync(string newUserName);
        Task<bool> UpdateDateOfBirthAsync(DateTime? dateOfBirth);

        // Администрирование
        Task<List<UserDto>?> GetUsersAsync();
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, string role);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private const string TOKEN_KEY = "auth_token";
        private const string USER_ID_KEY = "user_id";
        private const string USER_NAME_KEY = "user_name";
        private const string USER_ROLE_KEY = "user_role";
        private const string USER_EMAIL_KEY = "user_email";
        private const string REG_DATE_KEY = "reg_date";
        private const string LAST_LOGIN_KEY = "last_login";
        private const string DOB_KEY = "dob";
        private const string AVATAR_KEY = "avatar_url";

        public AuthService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public async Task<(bool Success, string ErrorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
                if (response.IsSuccessStatusCode)
                {
                    var authData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (authData?.Token != null)
                    {
                        await SaveAuthDataAsync(authData, email);
                        return (true, string.Empty);
                    }
                    return (false, "Не удалось получить токен");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                    if (errorObj.RootElement.TryGetProperty("message", out var msg))
                        return (false, msg.GetString() ?? "Неверный email или пароль");
                }
                catch { }
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RegisterAsync(string userName, string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", new { userName, email, password });
                if (response.IsSuccessStatusCode)
                    return (true, string.Empty);

                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                    if (errorObj.RootElement.TryGetProperty("message", out var msg))
                        return (false, msg.GetString() ?? "Ошибка регистрации");
                }
                catch { }
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task LogoutAsync()
        {
            var keys = new[] { TOKEN_KEY, USER_ID_KEY, USER_NAME_KEY, USER_ROLE_KEY, USER_EMAIL_KEY, REG_DATE_KEY, LAST_LOGIN_KEY, DOB_KEY, AVATAR_KEY };
            foreach (var key in keys)
                await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }

        public async Task<bool> IsAuthenticatedAsync() => !string.IsNullOrEmpty(await GetTokenAsync());

        public async Task<string?> GetTokenAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
        public async Task<string?> GetUserRoleAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_ROLE_KEY);
        public async Task<string?> GetUserIdAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_ID_KEY);
        public async Task<string?> GetUserNameAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_NAME_KEY);
        public async Task<string?> GetUserEmailAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_EMAIL_KEY);

        public async Task<DateTime?> GetRegistrationDateAsync()
        {
            var val = await _js.InvokeAsync<string?>("localStorage.getItem", REG_DATE_KEY);
            return string.IsNullOrEmpty(val) ? null : DateTime.Parse(val);
        }

        public async Task<DateTime?> GetLastLoginDateAsync()
        {
            var val = await _js.InvokeAsync<string?>("localStorage.getItem", LAST_LOGIN_KEY);
            return string.IsNullOrEmpty(val) ? null : DateTime.Parse(val);
        }

        public async Task<DateTime?> GetDateOfBirthAsync()
        {
            var val = await _js.InvokeAsync<string?>("localStorage.getItem", DOB_KEY);
            return string.IsNullOrEmpty(val) ? null : DateTime.Parse(val);
        }

        public async Task<string?> GetAvatarUrlAsync()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", AVATAR_KEY);
        }

        public async Task<bool> UploadAvatarAsync(Stream fileStream, string fileName)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                using var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", fileName);

                var request = new HttpRequestMessage(HttpMethod.Post, "api/users/me/avatar");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = content;

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<AvatarResponse>();
                if (result?.AvatarUrl != null)
                    await _js.InvokeVoidAsync("localStorage.setItem", AVATAR_KEY, result.AvatarUrl);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteAvatarAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                var request = new HttpRequestMessage(HttpMethod.Delete, "api/users/me/avatar");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    await _js.InvokeVoidAsync("localStorage.removeItem", AVATAR_KEY);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateUserNameAsync(string newUserName)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                var request = new HttpRequestMessage(HttpMethod.Put, "api/users/me/username");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { userName = newUserName });
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                await _js.InvokeVoidAsync("localStorage.setItem", USER_NAME_KEY, newUserName);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateDateOfBirthAsync(DateTime? dateOfBirth)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                var request = new HttpRequestMessage(HttpMethod.Put, "api/users/me/dateofbirth");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { dateOfBirth });
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                if (dateOfBirth.HasValue)
                    await _js.InvokeVoidAsync("localStorage.setItem", DOB_KEY, dateOfBirth.Value.ToString("o"));
                else
                    await _js.InvokeVoidAsync("localStorage.removeItem", DOB_KEY);
                return true;
            }
            catch { return false; }
        }

        public async Task<List<UserDto>?> GetUsersAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;
                var request = new HttpRequestMessage(HttpMethod.Get, "api/users");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<List<UserDto>>();
            }
            catch { return null; }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/users/{userId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string role)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/{userId}/role");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { userType = role });
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/{userId}/status");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { isActive });
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private async Task SaveAuthDataAsync(AuthResponseDto authData, string email)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, authData.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", USER_ID_KEY, authData.UserId);
            await _js.InvokeVoidAsync("localStorage.setItem", USER_NAME_KEY, authData.UserName);
            await _js.InvokeVoidAsync("localStorage.setItem", USER_ROLE_KEY, authData.UserType);
            await _js.InvokeVoidAsync("localStorage.setItem", USER_EMAIL_KEY, email);
            if (authData.RegistrationDate != default)
                await _js.InvokeVoidAsync("localStorage.setItem", REG_DATE_KEY, authData.RegistrationDate.ToString("o"));
            if (authData.LastLoginDate.HasValue)
                await _js.InvokeVoidAsync("localStorage.setItem", LAST_LOGIN_KEY, authData.LastLoginDate.Value.ToString("o"));
            if (authData.DateOfBirth.HasValue)
                await _js.InvokeVoidAsync("localStorage.setItem", DOB_KEY, authData.DateOfBirth.Value.ToString("o"));
            if (!string.IsNullOrEmpty(authData.AvatarUrl))
                await _js.InvokeVoidAsync("localStorage.setItem", AVATAR_KEY, authData.AvatarUrl);
        }

        private class AvatarResponse
        {
            public string? AvatarUrl { get; set; }
        }
    }
}