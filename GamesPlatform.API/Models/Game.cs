using System.Collections.Generic;  

namespace GamesPlatform.API.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public string? Logo { get; set; }

        // Внешние ключи
        public int GenreId { get; set; }
        public int DeveloperId { get; set; }

        // Навигационные свойства
        public Genre? Genre { get; set; }  
        public User? Developer { get; set; } 
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}