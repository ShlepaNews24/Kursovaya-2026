using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        public async Task RegisterAsync_CreatesUserAndReturnsDto()
        {
            var context = GetInMemoryContext();
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JwtSettings:SecretKey"])
                .Returns("SuperSecretKey1234567890!@#$%^&*()");
            configMock.Setup(c => c["JwtSettings:Issuer"])
                .Returns("TestIssuer");
            configMock.Setup(c => c["JwtSettings:Audience"])
                .Returns("TestAudience");
            configMock.Setup(c => c["JwtSettings:ExpiresInMinutes"])
                .Returns("60");

            var loggerMock = new Mock<ILogger<AuthService>>();
            var service = new AuthService(context, configMock.Object, loggerMock.Object);
            
            var dto = new RegisterDto 
            { 
                UserName = "Tester", 
                Email = "test@test.com", 
                Password = "StrongPass123!" 
            };

            var result = await service.RegisterAsync(dto);

            Assert.NotNull(result);
            Assert.NotEmpty(result.UserId);
            Assert.Equal("Tester", result.UserName);
            Assert.True(context.Users.Any(u => u.Email == dto.Email));
        }
    }
}