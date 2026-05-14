using System.Collections.Generic;

namespace GamesPlatform.API.Models
{
    public class Rating
    {
        public int RatingId { get; set; }
        public int RatingValue { get; set; } // 1-5

        // Внешние ключи
        public int UserId { get; set; }
        public int GameId { get; set; }

        // Навигационные свойства
        public User? User { get; set; } 
        public Game? Game { get; set; } 
    }
}