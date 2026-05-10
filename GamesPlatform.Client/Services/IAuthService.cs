// [НАЗНАЧЕНИЕ] Интерфейс сервиса авторизации
// [ФАЙЛ] Services/IAuthService.cs

using GamesPlatform.Client.Models; // ✅ Используем DTO из папки Models
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamesPlatform.Client.Services
{
    public interface IAuthService
    {
        // ========================================================================
        // Основные методы авторизации
        // ========================================================================
        Task<bool> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(string userName, string email, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();

        // ========================================================================
        // Получение данных пользователя
        // ========================================================================
        Task<string?> GetUserRoleAsync();
        Task<string?> GetUserEmailAsync();
        Task<string?> GetUserIdAsync();
        Task<string?> GetUserNameAsync();
        Task<string?> GetTokenAsync();
        
        Task<DateTime?> GetRegistrationDateAsync();
        Task<DateTime?> GetLastLoginDateAsync();
        Task<DateTime?> GetDateOfBirthAsync();

        // ========================================================================
        // Обновление профиля
        // ========================================================================
        Task<bool> UpdateUserNameAsync(string newUserName);
        Task<bool> UpdateDateOfBirthAsync(DateTime? dateOfBirth);

        // ========================================================================
        // Функции администратора
        // ========================================================================
        
        // ✅ Используем Models.UserDto, чтобы избежать ошибки CS0104
        Task<List<Models.UserDto>?> GetUsersAsync(); 
        
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, string role);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    }
}