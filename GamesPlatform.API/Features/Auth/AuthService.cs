// [ФАЙЛ] GamesPlatform.API/Features/Auth/AuthService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity; // ✅ Для IPasswordHasher
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Features.Auth
{
    public interface IAuthService
    {
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("⚠️ Validation failed: user not found or inactive {Email}", email);
                return false;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("⚠️ Validation failed: invalid password for {Email}", email);
                return false;
            }

            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email уже существует");
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                throw new Exception("Имя пользователя уже существует");

            var userType = (await _context.Users.CountAsync() == 0) ? "Admin" : "User";
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                UserType = userType,
                IsActive = true,
                RegistrationDate = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return new AuthResponseDto
            {
                UserId = user.UserId.ToString(),
                UserName = user.UserName,
                UserType = user.UserType,
                RegistrationDate = user.RegistrationDate
            };
        }
    }
}