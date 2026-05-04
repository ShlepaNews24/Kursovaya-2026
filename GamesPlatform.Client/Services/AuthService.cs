using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using GamesPlatform.Client.Models;

namespace GamesPlatform.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private readonly INotificationService _notify;

        private const string TOKEN_KEY = "auth_token";
        private const string ROLE_KEY = "user_role";
        private const string EMAIL_KEY = "user_email";
        private const string USER_ID_KEY = "user_id";
        private const string USER_NAME_KEY = "user_name";
        private const string REG_DATE_KEY = "reg_date";
        private const string LAST_LOGIN_KEY = "last_login";
        private const string DOB_KEY = "dob";

        public AuthService(HttpClient http, IJSRuntime js, INotificationService notify)
        {
            _http = http;
            _js = js;
            _notify = notify;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Login error: {error}", "error");
                    return false;
                }

                var authData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authData?.Token != null)
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, authData.Token);
                    await _js.InvokeVoidAsync("localStorage.setItem", ROLE_KEY, authData.UserType);
                    await _js.InvokeVoidAsync("localStorage.setItem", EMAIL_KEY, email);
                    
                    if (!string.IsNullOrEmpty(authData.UserId))
                        await _js.InvokeVoidAsync("localStorage.setItem", USER_ID_KEY, authData.UserId);
                    
                    if (!string.IsNullOrEmpty(authData.UserName))
                        await _js.InvokeVoidAsync("localStorage.setItem", USER_NAME_KEY, authData.UserName);
                    
                    await _js.InvokeVoidAsync("localStorage.setItem", REG_DATE_KEY, authData.RegistrationDate.ToString("o"));
                    if (authData.LastLoginDate.HasValue)
                        await _js.InvokeVoidAsync("localStorage.setItem", LAST_LOGIN_KEY, authData.LastLoginDate.Value.ToString("o"));
                    if (authData.DateOfBirth.HasValue)
                        await _js.InvokeVoidAsync("localStorage.setItem", DOB_KEY, authData.DateOfBirth.Value.ToString("o"));

                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);
                    _notify.Show("Login successful!", "success");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _notify.Show($"Network error: {ex.Message}", "error");
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string userName, string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", new { userName, email, password });
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Registration error: {error}", "error");
                    return false;
                }
                _notify.Show("Registration successful! Please login.", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Network error: {ex.Message}", "error");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", ROLE_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", EMAIL_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", USER_ID_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", USER_NAME_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", REG_DATE_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", LAST_LOGIN_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", DOB_KEY);
            _http.DefaultRequestHeaders.Authorization = null;
            _notify.Show("Logged out", "info");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }
            return false;
        }

        public async Task<string?> GetUserRoleAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", ROLE_KEY);
        public async Task<string?> GetUserEmailAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", EMAIL_KEY);
        public async Task<string?> GetUserIdAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_ID_KEY);
        public async Task<string?> GetUserNameAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", USER_NAME_KEY);
        public async Task<string?> GetTokenAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);

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

        public async Task<bool> UpdateUserNameAsync(string newUserName)
        {
            try
            {
                var response = await _http.PutAsJsonAsync("api/users/me/username", new { userName = newUserName });
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Update error: {error}", "error");
                    return false;
                }
                await _js.InvokeVoidAsync("localStorage.setItem", USER_NAME_KEY, newUserName);
                _notify.Show("Nickname updated!", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Network error: {ex.Message}", "error");
                return false;
            }
        }

        public async Task<bool> UpdateDateOfBirthAsync(DateTime? dateOfBirth)
        {
            try
            {
                var response = await _http.PutAsJsonAsync("api/users/me/dateofbirth", new { dateOfBirth });
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _notify.Show($"Update error: {error}", "error");
                    return false;
                }
                
                if (dateOfBirth.HasValue)
                    await _js.InvokeVoidAsync("localStorage.setItem", DOB_KEY, dateOfBirth.Value.ToString("o"));
                else
                    await _js.InvokeVoidAsync("localStorage.removeItem", DOB_KEY);
                
                _notify.Show("Date of birth updated!", "success");
                return true;
            }
            catch (Exception ex)
            {
                _notify.Show($"Network error: {ex.Message}", "error");
                return false;
            }
        }
    }
}