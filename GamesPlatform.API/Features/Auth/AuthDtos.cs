// [НАЗНАЧЕНИЕ] DTO для авторизации на сервере
// [ФАЙЛ] GamesPlatform.API/Features/Auth/AuthDtos.cs

using System.ComponentModel.DataAnnotations;

namespace GamesPlatform.API.Features.Auth
{
    // ========================================================================
    // [НАЗНАЧЕНИЕ] Данные для регистрации
    // ========================================================================
    public class RegisterDto
    {
        [Required, MinLength(3), MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] Данные для входа
    // ========================================================================
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] Ответ сервера после авторизации
    // [ВАЖНО] Содержит все данные пользователя для клиента
    // ========================================================================
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string UserType { get; set; } = string.Empty;
        
        // ✅ ID и Ник обязательны для работы профиля и комментариев
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    // ========================================================================
    // [НАЗНАЧЕНИЕ] Ответ при ошибке
    // ========================================================================
    public class AuthErrorDto
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}