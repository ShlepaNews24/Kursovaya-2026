// [НАЗНАЧЕНИЕ] Интерфейс сервиса авторизации
// [ФАЙЛ] Services/IAuthService.cs
// [ОБНОВЛЕНО] Добавлены методы для работы с UserId, UserName и сменой ника

namespace GamesPlatform.Client.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// [НАЗНАЧЕНИЕ] Вход пользователя в систему
        /// </summary>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Регистрация нового пользователя
        /// </summary>
        Task<bool> RegisterAsync(string userName, string email, string password);

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Выход из системы
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Проверка авторизации
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Получение роли пользователя
        /// </summary>
        Task<string?> GetUserRoleAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Получение email пользователя
        /// </summary>
        Task<string?> GetUserEmailAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Получение ID пользователя (из localStorage)
        /// ✅ НОВЫЙ МЕТОД
        /// </summary>
        Task<string?> GetUserIdAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Получение имени пользователя (ника)
        /// ✅ НОВЫЙ МЕТОД
        /// </summary>
        Task<string?> GetUserNameAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Получение токена
        /// </summary>
        Task<string?> GetTokenAsync();

        /// <summary>
        /// [НАЗНАЧЕНИЕ] Обновление имени пользователя на сервере
        /// ✅ НОВЫЙ МЕТОД
        /// </summary>
        Task<bool> UpdateUserNameAsync(string newUserName);
    }
}