using Microsoft.Extensions.Configuration;
using Moq;
using GamesPlatform.API.Features.Auth;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GamesPlatform.Tests
{
    public class AuthServiceTests
    {
        // Тест: Регистрация должна создавать пользователя и возвращать токен
        [Fact]
        public async Task RegisterAsync_CreatesUserAndReturnsToken()
        {
            // Arrange: Мокаем DbContext и IConfiguration
            var context = GetInMemoryContext();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c.GetSection("JwtSettings")["SecretKey"]).Returns("SuperSecretKey1234567890!@#$%^&*()");
            configMock.Setup(c => c.GetSection("JwtSettings")["Issuer"]).Returns("TestIssuer");
            configMock.Setup(c => c.GetSection("JwtSettings")["Audience"]).Returns("TestAudience");
            configMock.Setup(c => c.GetSection("JwtSettings")["ExpiresInMinutes"]).Returns("60");

            var service = new AuthService(context, configMock.Object);
            var dto = new RegisterDto { UserName = "Tester", Email = "test@test.com", Password = "StrongPass123!" };

            // Act
            var result = await service.RegisterAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token); // Проверяем, что токен сгенерирован
            Assert.True(context.Users.Any(u => u.Email == dto.Email)); // Проверяем, что пользователь сохранен в БД
        }

        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }
    }
}