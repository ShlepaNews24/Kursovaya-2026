using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamesPlatform.API.Models
{
    public class Game
    {
        [Key]
        public int GameId { get; set; }
        
        [Required, MaxLength(200)]
        public string GameTitle { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public DateTime ReleaseDate { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        
        public string? Logo { get; set; }
        
        public int GenreId { get; set; }
        public int DeveloperId { get; set; }
        
        [Required, MaxLength(500)]
        public string GameUrl { get; set; } = string.Empty;
        
        [ForeignKey("GenreId")]
        public Genre? Genre { get; set; }
        
        [ForeignKey("DeveloperId")]
        public User? Developer { get; set; }
        
        public ICollection<Comment>? Comments { get; set; } = new List<Comment>();
        public ICollection<Rating>? Ratings { get; set; } = new List<Rating>();
    }
}