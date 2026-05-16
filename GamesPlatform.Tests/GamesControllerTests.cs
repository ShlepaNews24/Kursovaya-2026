using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using GamesPlatform.API.Controllers;
using GamesPlatform.API.Models;
using GamesPlatform.API.Interfaces;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamesPlatform.Tests
{
    public class GamesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GamesController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GamesController>>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new GamesController(_mockUow.Object, _mockLogger.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetGames_ReturnsOkResult_WithListOfGames()
        {
            var expectedItems = new List<GameDto>
            {
                new GameDto { GameId = 1, GameTitle = "Test Game", GenreId = 1, DeveloperId = 1 }
            };

            _mockUow.Setup(u => u.GetPagedGamesAsync(1, 10, null, null))
                .ReturnsAsync((expectedItems, 1));

            // Настройка кеша, чтобы он не мешал (возвращаем false, чтобы выполнился реальный метод)
            object? cacheEntry = null;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
            _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            var query = new GamesQueryDto { PageNumber = 1, PageSize = 10 };
            var result = await _controller.GetGames(query);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<GameDto>>(okResult.Value);
            
            Assert.NotEmpty(pagedResult.Items);
            Assert.Contains(pagedResult.Items, g => g.GameTitle == "Test Game");
        }

        [Fact]
        public async Task PostGame_ReturnsCreatedAtActionResult_WhenValid()
        {
            var userIdClaim = new Claim(ClaimTypes.NameIdentifier, "1");
            var claimsIdentity = new ClaimsIdentity(new[] { userIdClaim }, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var newGameDto = new CreateGameDto
            {
                GameTitle = "New Game",
                GenreId = 1,
                GameUrl = "https://example.com",
                ReleaseDate = System.DateTime.UtcNow
            };

            var mockGenresRepo = new Mock<IRepository<Genre>>();
            mockGenresRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockUow.Setup(u => u.Genres).Returns(mockGenresRepo.Object);

            var mockGamesRepo = new Mock<IRepository<Game>>();
            mockGamesRepo.Setup(r => r.AddAsync(It.IsAny<Game>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.Games).Returns(mockGamesRepo.Object);
            
            _mockUow.Setup(u => u.SaveAsync()).ReturnsAsync(1);

            var result = await _controller.PostGame(newGameDto);

            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedDto = Assert.IsType<GameDto>(createdAtResult.Value);
            
            Assert.Equal("New Game", returnedDto.GameTitle);
            
            mockGenresRepo.Verify(r => r.ExistsAsync(1), Times.Once);
            mockGamesRepo.Verify(r => r.AddAsync(It.IsAny<Game>()), Times.Once);
            _mockUow.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
        {
            var mockGamesRepo = new Mock<IRepository<Game>>();
            mockGamesRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Game?)null);
            _mockUow.Setup(u => u.Games).Returns(mockGamesRepo.Object);

            var result = await _controller.GetGame(999);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}