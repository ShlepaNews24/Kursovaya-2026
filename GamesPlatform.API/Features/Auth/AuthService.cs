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
    // Интерфейс сервиса авторизации (для тестов и DI)
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

        // Внедрение зависимостей через конструктор
        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Проверка уникальности email
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Пользователь с таким email уже существует");

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                RegistrationDate = DateTime.UtcNow,
                IsActive = true,
                UserType = "User"
            };

            //Хеширование пароля перед сохранением в БД
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            //Генерация токена сразу после регистрации
            return GenerateToken(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Поиск пользователя по email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !user.IsActive)
                throw new Exception("Неверный email или пароль");

            // Верификация введенного пароля с хешем в БД
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new Exception("Неверный email или пароль");

            return GenerateToken(user);
        }

        private AuthResponseDto GenerateToken(User user)
        {
            // Чтение настроек JWT из appsettings.json
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"]!));

            // Формирование полезных данных токена (Claims)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType)
            };

            // Создание и сериализация JWT-токена
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiry
            };
        }
    }
}