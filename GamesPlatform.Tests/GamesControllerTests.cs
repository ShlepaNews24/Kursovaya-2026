using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using GamesPlatform.API.Controllers;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Xunit;
using System.Collections.Generic;  

namespace GamesPlatform.Tests
{
    public class GamesControllerTests
    {
        // Создание in-memory базы данных для тестов
        // Каждый тест получает изолированную БД
        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // [НАЗНАЧЕНИЕ] Тест: GET /api/games возвращает список игр
        [Fact]
        public async Task GetGames_ReturnsOkResult_WithListOfGames()
        {
            // arrange
            var context = GetInMemoryContext();
            
            // Сначала добавляем связанные сущности, на которые ссылается Game
            context.Genres.Add(new Genre { GenreId = 1, GenreName = "Action" });
            context.Users.Add(new User 
            { 
                UserId = 1, 
                UserName = "TestDev", 
                Email = "dev@test.com", 
                PasswordHash = "hash123" 
            });
            await context.SaveChangesAsync();  // Сохраняем, чтобы присвоились ID
            
            // Теперь добавляем игру с валидными внешними ключами
            context.Games.Add(new Game 
            { 
                GameId = 1, 
                GameTitle = "Test Game", 
                GenreId = 1,           // ← Ссылается на существующий Genre
                DeveloperId = 1,       // ← Ссылается на существующего User
                ReleaseDate = DateTime.UtcNow 
            });
            await context.SaveChangesAsync();

            var controller = new GamesController(context);

            // act
            var result = await controller.GetGames();

            // assert
            // Извлекаем список игр из ActionResult<T>.Value
            var games = Assert.IsAssignableFrom<IEnumerable<Game>>(result.Value);
            
            // Проверяем, что список не пустой и содержит нашу игру
            Assert.NotEmpty(games);
            Assert.Contains(games, g => g.GameTitle == "Test Game");
        }

        // Тест: POST создаёт игру и возвращает 201 Created
        [Fact]
        public async Task PostGame_ReturnsCreatedAtActionResult_WhenValid()
        {
            // arrange
            var context = GetInMemoryContext();
            var controller = new GamesController(context);
            
            // Предварительно создаём жанр и разработчика
            context.Genres.Add(new Genre { GenreId = 1, GenreName = "Action" });
            context.Users.Add(new User 
            { 
                UserId = 1, 
                UserName = "Dev", 
                Email = "dev@test.com", 
                PasswordHash = "hash" 
            });
            await context.SaveChangesAsync();

            var newGame = new Game 
            { 
                GameTitle = "New Game", 
                GenreId = 1, 
                DeveloperId = 1,
                ReleaseDate = DateTime.UtcNow
            };

            // act
            var result = await controller.PostGame(newGame);

            // assert
            // Проверяем статус 201 Created
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            
            // Проверяем, что в ответе вернулась созданная игра
            var returnedGame = Assert.IsType<Game>(createdAtResult.Value);
            Assert.Equal("New Game", returnedGame.GameTitle);
            
            // Проверяем, что игра сохранилась в БД
            Assert.True(await context.Games.AnyAsync(g => g.GameTitle == "New Game"));
        }

        // Тест: GET по несуществующему ID возвращает 404
        [Fact]
        public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
        {
            // arrange
            var context = GetInMemoryContext();
            var controller = new GamesController(context);

            // act
            var result = await controller.GetGame(999);

            // assert
            // Проверяем статус 404 Not Found
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}