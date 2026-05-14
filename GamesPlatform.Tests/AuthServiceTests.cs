using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // ✅ Добавлен using для ILogger
using Moq;
using GamesPlatform.API.Features.Auth;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using System.Threading.Tasks;

namespace GamesPlatform.Tests
{
    public class AuthServiceTests
    {
        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task RegisterAsync_CreatesUserAndReturnsToken()
        {
            // arrange
            var context = GetInMemoryContext();
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c.GetSection("JwtSettings")["SecretKey"])
                .Returns("SuperSecretKey1234567890!@#$%^&*()");
            configMock.Setup(c => c.GetSection("JwtSettings")["Issuer"])
                .Returns("TestIssuer");
            configMock.Setup(c => c.GetSection("JwtSettings")["Audience"])
                .Returns("TestAudience");
            configMock.Setup(c => c.GetSection("JwtSettings")["ExpiresInMinutes"])
                .Returns("60");

            // ✅ Мокаем ILogger<AuthService>
            var loggerMock = new Mock<ILogger<AuthService>>();

            // ✅ Передаём все 3 параметра в конструктор
            var service = new AuthService(context, configMock.Object, loggerMock.Object);
            
            var dto = new RegisterDto 
            { 
                UserName = "Tester", 
                Email = "test@test.com", 
                Password = "StrongPass123!" 
            };

            // act
            var result = await service.RegisterAsync(dto);

            // assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.True(context.Users.Any(u => u.Email == dto.Email));
        }
    }
}