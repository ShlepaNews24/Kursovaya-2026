using System.Collections.Generic;  
namespace GamesPlatform.API.Models
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;

        // Навигационное свойство
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}