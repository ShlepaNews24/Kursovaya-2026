using System.Collections.Generic;

namespace GamesPlatform.API.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Внешние ключи
        public int UserId { get; set; }
        public int GameId { get; set; }

        // Навигационные свойства
        public User? User { get; set; } 
        public Game? Game { get; set; } 
    }
}