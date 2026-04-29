namespace GamesPlatform.API.Features.Auth
{
    // Данные для регистрации нового пользователя
    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Данные для входа в систему
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Ответ сервера с JWT-токеном
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}