using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(string email, string password);
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ValidateCredentialsAsync(string email, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !await ValidateCredentialsAsync(email, password))
                throw new Exception("Invalid credentials");

            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                UserType = user.UserType,
                UserId = user.UserId.ToString(),
                UserName = user.UserName,
                RegistrationDate = user.RegistrationDate,
                LastLoginDate = user.LastLoginDate,
                DateOfBirth = user.DateOfBirth
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                throw new Exception("Username already exists");

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

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.IsActive) return false;
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed) return false;

            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForDevelopment1234567890!";
            var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "GamesPlatformAPI";
            var jwtAudience = _configuration["JwtSettings:Audience"] ?? "GamesPlatformClient";
            var expiryMinutes = _configuration.GetValue<int>("JwtSettings:ExpiresInMinutes", 60);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType),
                new Claim("user_type", user.UserType)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}