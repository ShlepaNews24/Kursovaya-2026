// Сервис авторизации с полной поддержкой профиля
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Пользователь с таким email уже существует");
            
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                throw new Exception("Пользователь с таким именем уже существует");

            var userType = (await _context.Users.CountAsync() == 0) ? "Admin" : "User";

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                UserType = userType, 
                IsActive = true,
                RegistrationDate = DateTime.UtcNow,
                LastLoginDate = null,
                DateOfBirth = null
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return GenerateToken(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !user.IsActive)
                throw new Exception("Неверный email или пароль");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new Exception("Неверный email или пароль");

            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateToken(user);
        }

        private AuthResponseDto GenerateToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType),
                new Claim("UserName", user.UserName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiry,
                UserType = user.UserType,
                UserId = user.UserId.ToString(),
                UserName = user.UserName,
                RegistrationDate = user.RegistrationDate,
                LastLoginDate = user.LastLoginDate,
                DateOfBirth = user.DateOfBirth
            };
        }
    }
}