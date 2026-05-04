// Интерфейс сервиса авторизации
namespace GamesPlatform.Client.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string email, string password);

        Task<bool> RegisterAsync(string userName, string email, string password);

        Task LogoutAsync();

        Task<bool> IsAuthenticatedAsync();

        Task<string?> GetUserRoleAsync();

        Task<string?> GetUserEmailAsync();

        Task<string?> GetUserIdAsync();

        Task<string?> GetUserNameAsync();

        Task<string?> GetTokenAsync();

        Task<bool> UpdateUserNameAsync(string newUserName);

        Task<DateTime?> GetRegistrationDateAsync();

        Task<DateTime?> GetLastLoginDateAsync();

        Task<DateTime?> GetDateOfBirthAsync();

        Task<bool> UpdateDateOfBirthAsync(DateTime? dateOfBirth);
    }
}