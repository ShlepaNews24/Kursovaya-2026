// Классы данных для обмена с серверным API
using System.Text.Json.Serialization;

namespace GamesPlatform.Client.Models
{
    // Запрос авторизации
    public class AuthRequestDto
    {
        public string? UserName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Ответ сервера после авторизации
    public class AuthResponseDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        
        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonPropertyName("userType")]
        public string UserType { get; set; } = string.Empty;
        
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
        
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }
        
        [JsonPropertyName("registrationDate")]
        public DateTime RegistrationDate { get; set; }
        
        [JsonPropertyName("lastLoginDate")]
        public DateTime? LastLoginDate { get; set; }
        
        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }
    }

    // Модель игры с описанием и URL
    public class GameDto
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }
        
        [JsonPropertyName("gameTitle")]
        public string GameTitle { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("releaseDate")]
        public DateTime ReleaseDate { get; set; }
        
        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; }
        
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
        
        [JsonPropertyName("genreId")]
        public int GenreId { get; set; }
        
        [JsonPropertyName("developerId")]
        public int DeveloperId { get; set; }
        
        [JsonPropertyName("gameUrl")]
        public string GameUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("genreName")]
        public string? GenreName { get; set; }
        
        [JsonPropertyName("developerName")]
        public string? DeveloperName { get; set; }
    }

    // Модель жанра
    public class GenreDto
    {
        [JsonPropertyName("genreId")]
        public int GenreId { get; set; }
        
        [JsonPropertyName("genreName")]
        public string GenreName { get; set; } = string.Empty;
    }

    // Модель комментария с поддержкой UserName
    public class CommentDto
    {
        [JsonPropertyName("commentId")]
        public int CommentId { get; set; }
        
        [JsonPropertyName("commentText")]
        public string CommentText { get; set; } = string.Empty;
        
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
        
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
        
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }
        
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }
    }

    // DTO для отправки оценки
    public class RatingDto
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }
        
        [JsonPropertyName("ratingValue")]
        public int RatingValue { get; set; }
    }

    // DTO для получения сводки рейтинга
    public class RatingSummaryDto
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }
        
        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }
        
        [JsonPropertyName("totalVotes")]
        public int TotalVotes { get; set; }
        
        [JsonPropertyName("userRating")]
        public int? UserRating { get; set; }
    }

    // DTO для смены ника
    public class UpdateUsernameDto
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;
    }

    // DTO для смены даты рождения
    public class UpdateDateOfBirthDto
    {
        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }
    }
}