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
        
        Task<List<UserDto>?> GetUsersAsync();
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, string role);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    }
    
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}